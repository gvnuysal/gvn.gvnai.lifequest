using System.Text.Json;
using System.Text.Json.Serialization;
using Gvn.GvnFramework.Core.Results;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Admin;
using LifeQuest.Domain.Identity;

namespace LifeQuest.Application.Admin;

/// <summary>Admin işlemini yapan hesabı çözer ve denetim kaydını işlemle aynı unit of work'e ekler.</summary>
public sealed class AdminAuditWriter(
    IUserContext user,
    IUserAccountRepository accounts,
    IAdminAuditRepository audit,
    TimeProvider clock)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private UserAccount? _actor;

    public Guid ActorId => user.UserId;

    public async Task<UserAccount> GetActorAsync(CancellationToken cancellationToken)
        => _actor ??= await accounts.GetByIdAsync(user.UserId, cancellationToken)
                      ?? throw new InvalidOperationException("Admin hesabı bulunamadı.");

    public async Task RecordAsync(
        AdminAction action, AdminTargetType targetType, Guid? targetId, string targetLabel,
        string? reason, object? details, CancellationToken cancellationToken)
    {
        var actor = await GetActorAsync(cancellationToken);
        await audit.AddAsync(AdminAuditEntry.Create(
            actor.Id, actor.Email, action, targetType, targetId, targetLabel, reason,
            details is null ? null : JsonSerializer.Serialize(details, Json),
            clock.GetUtcNow().UtcDateTime), cancellationToken);
    }
}

public static class AdminErrors
{
    public static readonly Error SelfAction =
        Error.Conflict("ADMIN_SELF_ACTION", "Bu işlemi kendi hesabına uygulayamazsın.");

    public static readonly Error TargetIsAdmin =
        Error.Conflict("ADMIN_TARGET_IS_ADMIN", "Admin rolündeki bir hesap askıya alınamaz veya silinemez. Önce admin rolünü kaldır.");

    public static readonly Error LastAdmin =
        Error.Conflict("ADMIN_LAST_ADMIN", "Son admin'in rolü kaldırılamaz.");

    public static readonly Error BootstrapAdmin =
        Error.Conflict("ADMIN_BOOTSTRAP", "Bu hesap Admin:BootstrapEmails ayarında tanımlı; rolü uygulama açılışında yeniden verilir. Önce ayardan çıkar.");

    public static readonly Error ConfirmEmailMismatch =
        Error.Validation("ConfirmEmail", "Onay için hesabın e-posta adresini aynen yazmalısın.");

    public static readonly Error StaleVersion =
        Error.Conflict("ADMIN_STALE_VERSION", "Bu kayıt sen düzenlerken değişti. Sayfayı yenileyip tekrar dene.");

    public static readonly Error TemplateNotFound =
        Error.NotFound("TEMPLATE_NOT_FOUND", "Template bulunamadı.");

    public static readonly Error TemplateCodeTaken =
        Error.Conflict("TEMPLATE_CODE_TAKEN", "Bu kodla bir template zaten var.");

    public static readonly Error ApprovalNeedsNote =
        Error.Validation("Note", "Güvenlik kuralı ihlali olan bir template'i onaylamak veya engellemek için gerekçe yazmalısın.");

    public static Error UnknownInterests(IEnumerable<Guid> ids) =>
        Error.Validation("InterestIds", $"Bilinmeyen ilgi alanı: {string.Join(", ", ids)}");
}
