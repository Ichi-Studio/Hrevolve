using Hrevolve.Domain.Common;

namespace Hrevolve.Domain.Identity;

public class RevokedAccessToken : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string Jti { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime RevokedAt { get; private set; } = DateTime.UtcNow;
    public string? RevokedByIp { get; private set; }

    private RevokedAccessToken() { }

    public static RevokedAccessToken Revoke(Guid tenantId, Guid userId, string jti, DateTime expiresAt, string? ipAddress)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("tenantId不能为空", nameof(tenantId));
        if (userId == Guid.Empty) throw new ArgumentException("userId不能为空", nameof(userId));
        if (string.IsNullOrWhiteSpace(jti)) throw new ArgumentException("jti不能为空", nameof(jti));

        return new RevokedAccessToken
        {
            TenantId = tenantId,
            UserId = userId,
            Jti = jti.Trim(),
            ExpiresAt = expiresAt,
            RevokedByIp = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim()
        };
    }
}

