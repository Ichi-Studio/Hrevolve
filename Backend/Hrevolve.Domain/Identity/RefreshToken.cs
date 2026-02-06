using Hrevolve.Domain.Common;

namespace Hrevolve.Domain.Identity;

public class RefreshToken : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? CreatedByUserAgent { get; private set; }
    public string? RevokedByIp { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Issue(
        Guid tenantId,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        string? ipAddress,
        string? userAgent)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("tenantId不能为空", nameof(tenantId));
        if (userId == Guid.Empty) throw new ArgumentException("userId不能为空", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new ArgumentException("tokenHash不能为空", nameof(tokenHash));

        return new RefreshToken
        {
            TenantId = tenantId,
            UserId = userId,
            TokenHash = tokenHash.Trim(),
            ExpiresAt = expiresAt,
            CreatedByIp = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            CreatedByUserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim()
        };
    }

    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;
    public bool IsRevoked => RevokedAt.HasValue;

    public void MarkUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    public void Revoke(Guid? replacedByTokenId, string? ipAddress)
    {
        if (RevokedAt.HasValue) return;
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenId = replacedByTokenId;
        RevokedByIp = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
    }
}

