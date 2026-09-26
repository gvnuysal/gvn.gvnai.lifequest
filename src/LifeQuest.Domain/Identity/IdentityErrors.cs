using Gvn.GvnFramework.Core.Results;
using LifeQuest.Domain.Localization;

namespace LifeQuest.Domain.Identity;

public static class IdentityErrors
{
    public static Error Underage =>
        Error.Validation("UNDERAGE", Text.Of(
            $"LifeQuest {UserAccount.MinimumAge} yaş ve üzeri kullanıcılar içindir.",
            $"LifeQuest is for users aged {UserAccount.MinimumAge} and over."));

    public static Error EmailTaken =>
        Error.Conflict("EMAIL_TAKEN", Text.Of("Bu e-posta adresiyle kayıtlı bir hesap zaten var.", "An account with this email already exists."));

    public static Error InvalidCredentials =>
        Error.Unauthorized("INVALID_CREDENTIALS", Text.Of("E-posta veya şifre hatalı.", "Email or password is incorrect."));

    public static Error AccountLocked =>
        Error.Unauthorized("ACCOUNT_LOCKED", Text.Of(
            "Çok fazla hatalı deneme yapıldı. Lütfen daha sonra tekrar deneyin.",
            "Too many failed attempts. Please try again later."));

    public static Error InvalidRefreshToken =>
        Error.Unauthorized("INVALID_REFRESH_TOKEN", Text.Of(
            "Oturum geçersiz veya süresi dolmuş. Lütfen tekrar giriş yapın.",
            "Your session is invalid or has expired. Please sign in again."));

    public static Error AccountSuspended(DateTime? untilUtc) => Error.Unauthorized("ACCOUNT_SUSPENDED",
        untilUtc is null
            ? Text.Of("Hesabın askıya alındı. Destek ekibiyle iletişime geçebilirsin.",
                "Your account has been suspended. You can contact the support team.")
            : Text.Format(
                $"Hesabın {untilUtc:g} (UTC) tarihine kadar askıya alındı.",
                $"Your account is suspended until {untilUtc:g} (UTC)."));

    public static Error AccountNotFound =>
        Error.NotFound("ACCOUNT_NOT_FOUND", Text.Of("Hesap bulunamadı.", "Account not found."));

    public static Error UnsupportedLanguage =>
        Error.Validation("Language", Text.Of("Desteklenen diller: tr, en.", "Supported languages: tr, en."));
}
