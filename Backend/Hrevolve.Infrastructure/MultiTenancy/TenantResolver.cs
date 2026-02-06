using Hrevolve.Domain.Tenants;
using Hrevolve.Infrastructure.Persistence;
using Hrevolve.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Hrevolve.Infrastructure.MultiTenancy;

/// <summary>
/// 租户解析器实现
/// </summary>
public class TenantResolver : ITenantResolver
{
    private readonly HrevolveDbContext _context;
    private readonly IDistributedCache _cache;
    private const string CacheKeyPrefix = "tenant:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    private static long _cacheDisabledUntilTicks;
    
    public TenantResolver(HrevolveDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }
    
    /// <summary>
    /// 根据标识（域名或代码）解析租户
    /// </summary>
    public async Task<TenantInfo?> ResolveAsync(string identifier)
    {
        // 先从缓存获取
        var cacheKey = $"{CacheKeyPrefix}identifier:{identifier}";
        if (IsCacheAvailable())
        {
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cached))
                {
                    return JsonSerializer.Deserialize<TenantInfo>(cached);
                }
            }
            catch
            {
                DisableCacheTemporarily();
            }
        }
        
        // 从数据库查询（忽略租户过滤器）
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Code == identifier || t.Domain == identifier)
            .FirstOrDefaultAsync();
        
        if (tenant == null) return null;
        
        var tenantInfo = MapToTenantInfo(tenant);
        
        // 缓存结果
        if (IsCacheAvailable())
        {
            try
            {
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(tenantInfo),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration });
            }
            catch
            {
                DisableCacheTemporarily();
            }
        }
        
        return tenantInfo;
    }
    
    /// <summary>
    /// 根据ID获取租户
    /// </summary>
    public async Task<TenantInfo?> GetByIdAsync(Guid tenantId)
    {
        var cacheKey = $"{CacheKeyPrefix}id:{tenantId}";
        if (IsCacheAvailable())
        {
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cached))
                {
                    return JsonSerializer.Deserialize<TenantInfo>(cached);
                }
            }
            catch
            {
                DisableCacheTemporarily();
            }
        }
        
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId);
        
        if (tenant == null) return null;
        
        var tenantInfo = MapToTenantInfo(tenant);
        
        if (IsCacheAvailable())
        {
            try
            {
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(tenantInfo),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration });
            }
            catch
            {
                DisableCacheTemporarily();
            }
        }
        
        return tenantInfo;
    }

    private static bool IsCacheAvailable()
    {
        var disabledUntil = Interlocked.Read(ref _cacheDisabledUntilTicks);
        return DateTime.UtcNow.Ticks >= disabledUntil;
    }

    private static void DisableCacheTemporarily()
    {
        var until = DateTime.UtcNow.AddMinutes(2).Ticks;
        Interlocked.Exchange(ref _cacheDisabledUntilTicks, until);
    }
    
    private static TenantInfo MapToTenantInfo(Tenant tenant)
    {
        return new TenantInfo
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Code = tenant.Code,
            Domain = tenant.Domain,
            ConnectionString = tenant.ConnectionString,
            IsActive = tenant.Status == TenantStatus.Active,
            Settings = new Shared.MultiTenancy.TenantSettings
            {
                Timezone = tenant.Settings.Timezone,
                Locale = tenant.Settings.Locale,
                Currency = tenant.Settings.Currency,
                MaxEmployees = tenant.Settings.MaxEmployees,
                EnableMfa = tenant.Settings.EnableMfa,
                EnableSso = tenant.Settings.EnableSso
            }
        };
    }
}
