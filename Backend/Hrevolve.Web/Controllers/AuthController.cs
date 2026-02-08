namespace Hrevolve.Web.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator, Hrevolve.Infrastructure.Persistence.HrevolveDbContext context, IConfiguration configuration) : ControllerBase
{
    
    /// <summary>
    /// 用户名密码登录
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        // 补充IP地址
        var enrichedCommand = command with
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
        
        var result = await mediator.Send(enrichedCommand, cancellationToken);
        
        if (result.IsFailure)
        {
            return BadRequest(new { code = result.ErrorCode, message = result.Error });
        }

        var login = result.Value!;
        var expiresIn = (int)Math.Max(0, (login.ExpiresAt - DateTime.UtcNow).TotalSeconds);

        if (!string.IsNullOrWhiteSpace(login.RefreshToken))
        {
            var refreshTokenDays = Math.Max(1, configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 14);
            var ip = enrichedCommand.IpAddress;
            var userAgent = Request.Headers.UserAgent.ToString();

            var user = await context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == login.UserId, cancellationToken);

            if (user != null)
            {
                var refreshTokenEntity = RefreshToken.Issue(
                    user.TenantId,
                    user.Id,
                    HashToken(login.RefreshToken),
                    DateTime.UtcNow.AddDays(refreshTokenDays),
                    ip,
                    userAgent);

                await context.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        return Ok(new
        {
            accessToken = login.AccessToken,
            refreshToken = login.RefreshToken,
            expiresIn,
            userId = login.UserId,
            userName = login.UserName,
            requiresMfa = login.RequiresMfa
        });
    }
    
    /// <summary>
    /// 刷新Token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { code = "VALIDATION_ERROR", message = "RefreshToken不能为空" });
        }

        var now = DateTime.UtcNow;
        var refreshTokenHash = HashToken(request.RefreshToken);

        var tokenEntity = await context.RefreshTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == refreshTokenHash, cancellationToken);

        if (tokenEntity == null || tokenEntity.IsRevoked || tokenEntity.IsExpired)
        {
            return Unauthorized(new { code = "INVALID_REFRESH_TOKEN", message = "RefreshToken无效或已过期" });
        }

        var user = await context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == tokenEntity.UserId, cancellationToken);

        if (user == null)
        {
            return Unauthorized(new { code = "INVALID_REFRESH_TOKEN", message = "RefreshToken无效" });
        }

        if (user.Status != Domain.Identity.UserStatus.Active)
        {
            return Unauthorized(new { code = "ACCOUNT_DISABLED", message = "账户已被禁用" });
        }

        tokenEntity.MarkUsed();

        var permissions = await GetPermissionsAsync(user.Id, cancellationToken);
        var accessToken = GenerateJwtToken(user, permissions, out var accessTokenExpiresAt);

        var refreshTokenDays = Math.Max(1, configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 14);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var newRefreshToken = GenerateSecureRefreshToken();
        var newRefreshTokenEntity = RefreshToken.Issue(
            user.TenantId,
            user.Id,
            HashToken(newRefreshToken),
            now.AddDays(refreshTokenDays),
            ip,
            userAgent);

        tokenEntity.Revoke(newRefreshTokenEntity.Id, ip);

        await context.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var expiresIn = (int)Math.Max(0, (accessTokenExpiresAt - DateTime.UtcNow).TotalSeconds);
        return Ok(new
        {
            accessToken,
            refreshToken = newRefreshToken,
            expiresIn,
            userId = user.Id,
            userName = user.Username,
            requiresMfa = false
        });
    }
    
    /// <summary>
    /// 登出
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userIdRaw) || !Guid.TryParse(userIdRaw, out var userId))
        {
            return Unauthorized();
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var jwt = ReadBearerToken(Request.Headers.Authorization.ToString());

        if (jwt != null && !string.IsNullOrWhiteSpace(jwt.Id))
        {
            var tenantIdRaw = User.FindFirst("tenant_id")?.Value;
            var tenantId = Guid.TryParse(tenantIdRaw, out var parsedTenantId) ? parsedTenantId : Guid.Empty;

            if (tenantId == Guid.Empty)
            {
                var user = await context.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
                tenantId = user?.TenantId ?? Guid.Empty;
            }

            if (tenantId != Guid.Empty)
            {
                var exists = await context.RevokedAccessTokens.IgnoreQueryFilters()
                    .AnyAsync(x => x.Jti == jwt.Id, cancellationToken);

                if (!exists)
                {
                    var revoked = RevokedAccessToken.Revoke(tenantId, userId, jwt.Id, jwt.ValidTo, ip);
                    await context.RevokedAccessTokens.AddAsync(revoked, cancellationToken);
                }
            }
        }

        var activeRefreshTokens = await context.RefreshTokens.IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var rt in activeRefreshTokens)
        {
            rt.Revoke(null, ip);
        }

        await context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Logout successful" });
    }
    
    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userIdRaw) || !Guid.TryParse(userIdRaw, out var userId))
        {
            return Unauthorized();
        }

        var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return Unauthorized();

        var permissions = User.FindAll("permission").Select(c => c.Value).Distinct().ToList();
        var roles = await (
            from ur in context.UserRoles.IgnoreQueryFilters()
            where ur.UserId == userId
            join r in context.Roles.IgnoreQueryFilters() on ur.RoleId equals r.Id
            select r.Name
        ).ToListAsync(cancellationToken);

        if (permissions.Contains(Permissions.SystemAdmin))
        {
            roles = roles.Contains("Admin") ? roles : ["Admin", ..roles];
        }

        var displayName = user.Username;
        if (user.EmployeeId.HasValue)
        {
            var employee = await context.Employees.IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == user.EmployeeId.Value, cancellationToken);

            if (employee != null)
            {
                displayName = employee.FullName;
            }
        }

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            displayName,
            roles,
            permissions,
            tenantId = user.TenantId,
            employeeId = user.EmployeeId
        });
    }

    private async Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roleIds = await context.UserRoles.IgnoreQueryFilters()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        var permissions = await context.Set<RolePermission>().IgnoreQueryFilters()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionCode)
            .Distinct()
            .ToListAsync(cancellationToken);

        return permissions;
    }

    private string GenerateJwtToken(User user, IReadOnlyList<string> permissions, out DateTime expiresAt)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var accessTokenMinutes = Math.Max(1, jwtSettings.GetValue<int?>("AccessTokenMinutes") ?? 120);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        expiresAt = DateTime.UtcNow.AddMinutes(accessTokenMinutes);

        var claims = new List<Claim>
        {
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, user.Email),
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", user.TenantId.ToString()),
            new("username", user.Username)
        };

        if (user.EmployeeId.HasValue)
        {
            claims.Add(new Claim("employee_id", user.EmployeeId.Value.ToString()));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateSecureRefreshToken()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static System.IdentityModel.Tokens.Jwt.JwtSecurityToken? ReadBearerToken(string authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader)) return null;
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;

        var raw = authorizationHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(raw)) return null;

        try
        {
            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(raw);
        }
        catch
        {
            return null;
        }
    }
}

public record RefreshTokenRequest(string RefreshToken);
