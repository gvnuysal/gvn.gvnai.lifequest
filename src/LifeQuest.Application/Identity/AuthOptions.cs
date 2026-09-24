namespace LifeQuest.Application.Identity;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public int RefreshTokenDays { get; set; } = 30;
}

/// <summary>
/// Bu e-postalara sahip hesaplar admin rolü alır (ürün metrikleri paneli): mevcut hesaplar açılışta,
/// yeni hesaplar kayıt anında.
/// </summary>
public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public string[] BootstrapEmails { get; set; } = [];

    public bool IsBootstrapAdmin(string normalizedEmail)
        => BootstrapEmails.Any(e => string.Equals(e.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase));
}

public sealed record AuthTokensDto(
    Guid UserId,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
