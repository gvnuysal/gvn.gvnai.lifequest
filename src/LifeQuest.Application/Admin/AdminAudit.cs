using System.Text.Json;
using System.Text.Json.Serialization;
using Gvn.GvnFramework.Core.Results;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Admin;
using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Localization;

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
    public static Error SelfAction =>
        Error.Conflict("ADMIN_SELF_ACTION", Text.Of("Bu işlemi kendi hesabına uygulayamazsın.", "You can't apply this action to your own account."));

    public static Error TargetIsAdmin =>
        Error.Conflict("ADMIN_TARGET_IS_ADMIN", Text.Of("Admin rolündeki bir hesap askıya alınamaz veya silinemez. Önce admin rolünü kaldır.", "An admin account can't be suspended or deleted. Remove the admin role first."));

    public static Error LastAdmin =>
        Error.Conflict("ADMIN_LAST_ADMIN", Text.Of("Son admin'in rolü kaldırılamaz.", "The last admin's role can't be removed."));

    public static Error BootstrapAdmin =>
        Error.Conflict("ADMIN_BOOTSTRAP", Text.Of("Bu hesap Admin:BootstrapEmails ayarında tanımlı; rolü uygulama açılışında yeniden verilir. Önce ayardan çıkar.", "This account is listed in Admin:BootstrapEmails; its role is granted again at startup. Remove it from the setting first."));

    public static Error ConfirmEmailMismatch =>
        Error.Validation("ConfirmEmail", Text.Of("Onay için hesabın e-posta adresini aynen yazmalısın.", "To confirm, type the account's email address exactly."));

    public static Error StaleVersion =>
        Error.Conflict("ADMIN_STALE_VERSION", Text.Of("Bu kayıt sen düzenlerken değişti. Sayfayı yenileyip tekrar dene.", "This record changed while you were editing. Refresh the page and try again."));

    public static Error TemplateNotFound =>
        Error.NotFound("TEMPLATE_NOT_FOUND", Text.Of("Template bulunamadı.", "Template not found."));

    public static Error TemplateCodeTaken =>
        Error.Conflict("TEMPLATE_CODE_TAKEN", Text.Of("Bu kodla bir template zaten var.", "A template with this code already exists."));

    public static Error ApprovalNeedsNote =>
        Error.Validation("Note", Text.Of("Güvenlik kuralı ihlali olan bir template'i onaylamak veya engellemek için gerekçe yazmalısın.", "Write a reason to approve or block a template that violates a safety rule."));

    public static Error UnknownTemplates(IEnumerable<Guid> ids) =>
        Error.Validation("TemplateIds", Text.Of($"Bilinmeyen görev: {string.Join(", ", ids)}", $"Unknown quest: {string.Join(", ", ids)}"));

    public static Error UnknownInterests(IEnumerable<Guid> ids) =>
        Error.Validation("InterestIds", Text.Of($"Bilinmeyen ilgi alanı: {string.Join(", ", ids)}", $"Unknown interest: {string.Join(", ", ids)}"));
}
