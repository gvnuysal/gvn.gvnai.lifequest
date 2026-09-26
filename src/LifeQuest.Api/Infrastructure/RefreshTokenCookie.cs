using System.Text.Json.Serialization;
using LifeQuest.Application.Identity;
using Microsoft.Extensions.Options;

namespace LifeQuest.Api.Infrastructure;

public sealed class RefreshCookieOptions
{
    public const string SectionName = "Auth:RefreshCookie";

    public string Name { get; set; } = "lq_refresh";

    /// <summary>Çerez yalnızca /auth uçlarına gider; diğer API isteklerinde taşınmaz.</summary>
    public string Path { get; set; } = "/api/v1/auth";

    /// <summary>Yalnızca HTTPS olmayan yerel testlerde kapatılır.</summary>
    public bool Secure { get; set; } = true;
}

/// <summary>
/// Refresh token HttpOnly çerezde taşınır: JavaScript (ve olası bir XSS) token'ı okuyamaz, yanıt gövdesinde de dönmez.
/// SameSite=Strict: UI ve API aynı sitenin alt alan adları (lifequesttest / lifequesttestapi.gvnaitech.com) olduğu için
/// çerez gönderilir, başka sitelerden gelen istekler çerezi taşımaz. /refresh yalnızca JSON gövde kabul ettiğinden
/// düz form ile tetiklenemez; tetiklense de yalnızca token'ı döndürür (rotasyon), yanıtı saldırgan okuyamaz.
/// </summary>
public sealed class RefreshTokenCookie(IOptions<RefreshCookieOptions> options)
{
    private readonly RefreshCookieOptions _options = options.Value;

    public string? Read(HttpRequest request)
        => request.Cookies.TryGetValue(_options.Name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    public void Write(HttpResponse response, AuthTokensDto tokens)
        => response.Cookies.Append(_options.Name, tokens.RefreshToken, Build(new DateTimeOffset(tokens.RefreshTokenExpiresAt, TimeSpan.Zero)));

    public void Clear(HttpResponse response)
        => response.Cookies.Delete(_options.Name, Build(expires: null));

    private CookieOptions Build(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = _options.Secure,
        SameSite = SameSiteMode.Strict,
        Path = _options.Path,
        Expires = expires,
        IsEssential = true
    };
}

/// <summary>
/// Giriş/kayıt/yenileme yanıtı. Web'de refresh token yalnızca çerezdedir, gövdede yalnızca bitiş zamanı döner;
/// <see cref="RefreshToken"/> yalnızca native istemciye yazılır.
/// </summary>
public sealed record AuthSessionResponse(
    Guid UserId,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? RefreshToken = null)
{
    public static AuthSessionResponse From(AuthTokensDto tokens)
        => new(tokens.UserId, tokens.AccessToken, tokens.AccessTokenExpiresAt, tokens.RefreshTokenExpiresAt);

    public static AuthSessionResponse ForNativeClient(AuthTokensDto tokens)
        => From(tokens) with { RefreshToken = tokens.RefreshToken };
}
