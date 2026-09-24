using Gvn.GvnFramework.Core.Results;

namespace LifeQuest.Domain.Identity;

public static class IdentityErrors
{
    public static readonly Error Underage =
        Error.Validation("UNDERAGE", $"LifeQuest {UserAccount.MinimumAge} yaş ve üzeri kullanıcılar içindir.");

    public static readonly Error EmailTaken =
        Error.Conflict("EMAIL_TAKEN", "Bu e-posta adresiyle kayıtlı bir hesap zaten var.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("INVALID_CREDENTIALS", "E-posta veya şifre hatalı.");

    public static readonly Error AccountLocked =
        Error.Unauthorized("ACCOUNT_LOCKED", "Çok fazla hatalı deneme yapıldı. Lütfen daha sonra tekrar deneyin.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("INVALID_REFRESH_TOKEN", "Oturum geçersiz veya süresi dolmuş. Lütfen tekrar giriş yapın.");

    public static readonly Error AccountNotFound =
        Error.NotFound("ACCOUNT_NOT_FOUND", "Hesap bulunamadı.");
}
