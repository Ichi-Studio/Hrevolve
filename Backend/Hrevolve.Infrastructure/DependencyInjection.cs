using Hrevolve.Infrastructure.MultiTenancy;
using Hrevolve.Infrastructure.Persistence.Repositories;

namespace Hrevolve.Infrastructure;

/// <summary>
/// 基础设施层依赖注入配置
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 数据库上下文
        services.AddDbContext<HrevolveDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(HrevolveDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(3);
                });
        });
        
        // Redis缓存
        var redisConnection = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnection) ||
            string.Equals(redisConnection, "disabled", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            var cfg = redisConnection.Contains("connectTimeout=", StringComparison.OrdinalIgnoreCase)
                ? redisConnection
                : $"{redisConnection},connectTimeout=500,syncTimeout=500,abortConnect=false";

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cfg;
                options.InstanceName = "Hrevolve:";
            });
        }
        
        // 多租户
        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped<ITenantResolver, TenantResolver>();
        
        // 当前用户
        services.AddSingleton<ICurrentUserAccessor, CurrentUserAccessor>();
        
        // 工作单元
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // 数据库初始化器
        services.AddScoped<DbInitializer>();
        services.AddScoped<DemoDataSeeder>();
        
        // 仓储
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        
        return services;
    }
}
