using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Gvn.GvnFramework.Security.Abstractions;
using Gvn.GvnFramework.Security.Configuration;
using LifeQuest.Domain.Identity;
using Microsoft.Extensions.Options;

namespace LifeQuest.Application.Identity;

/// <summary>
/// Kısa ömürlü access token (framework <see cref="ITokenService"/>) + rotasyonlu refresh token üretir.
/// Refresh token'ın yalnızca SHA-256 özeti veritabanında saklanır.
/// </summary>
public sealed class AuthTokenIssuer(
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokens,
    IOptions<JwtOptions> jwtOptions,
    IOptions<AuthOptions> authOptions,
    TimeProvider clock)
{
    public async Task<(AuthTokensDto Tokens, RefreshToken Entity)> IssueAsync(
        UserAccount account, Guid? familyId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Email, account.Email),
            new(ClaimTypes.Name, account.DisplayName),
            new(ClaimTypes.Role, account.Role),
            new("jti", Guid.NewGuid().ToString("N"))
        ];

        var accessToken = tokenService.GenerateToken(claims);

        var rawRefreshToken = Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
        var refreshToken = RefreshToken.Issue(
            account.Id, Hash(rawRefreshToken), familyId ?? Guid.NewGuid(), now,
            TimeSpan.FromDays(authOptions.Value.RefreshTokenDays));

        await refreshTokens.AddAsync(refreshToken, cancellationToken);

        var tokens = new AuthTokensDto(
            account.Id,
            accessToken,
            now.AddMinutes(jwtOptions.Value.ExpiryMinutes),
            rawRefreshToken,
            refreshToken.ExpiresAt);

        return (tokens, refreshToken);
    }

    public static string Hash(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
