using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Notifications;
using LifeQuest.Domain.Localization;

namespace LifeQuest.Application.Notifications;

public sealed record PushSettingsDto(bool Enabled, string? PublicKey, int Devices);

public static class PushErrors
{
    public static Error NotConfigured =>
        Error.Conflict("PUSH_NOT_CONFIGURED", Text.Of("Anlık bildirimler bu sunucuda etkin değil.", "Push notifications aren't enabled on this server."));

    public static Error NoDevices =>
        Error.Conflict("PUSH_NO_DEVICES", Text.Of("Bildirim alacak bir cihaz yok. Önce bu tarayıcıda bildirimlere izin ver.", "No device to notify. Allow notifications in this browser first."));
}

// ── Ayarlar ───────────────────────────────────────────────────────────────────

public sealed record GetPushSettingsQuery : IQuery<PushSettingsDto>;

internal sealed class GetPushSettingsQueryHandler(IPushSender sender, IPushSubscriptionRepository subscriptions, IUserContext user)
    : IQueryHandler<GetPushSettingsQuery, PushSettingsDto>
{
    public async Task<Result<PushSettingsDto>> Handle(GetPushSettingsQuery query, CancellationToken cancellationToken)
        => Result<PushSettingsDto>.Ok(new PushSettingsDto(
            sender.IsConfigured,
            sender.PublicKey,
            (await subscriptions.GetForUserAsync(user.UserId, cancellationToken)).Count));
}

// ── Abonelik ──────────────────────────────────────────────────────────────────

/// <summary>Tarayıcının <c>PushSubscription.toJSON()</c> çıktısı: endpoint + keys.p256dh + keys.auth.</summary>
public sealed record SubscribePushCommand(string Endpoint, string P256dh, string Auth) : ICommand<PushSettingsDto>;

public sealed class SubscribePushCommandValidator : AbstractValidator<SubscribePushCommand>
{
    public SubscribePushCommandValidator()
    {
        // Push servisleri HTTPS adres verir; başka bir şema sunucuyu keyfi adrese istek atmaya zorlayabilirdi (SSRF).
        RuleFor(x => x.Endpoint).NotEmpty().MaximumLength(2048)
            .Must(e => Uri.TryCreate(e, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && !uri.IsLoopback)
            .WithMessage(_ => Text.Of("Geçersiz push adresi.", "Invalid push endpoint."));
        RuleFor(x => x.P256dh).NotEmpty().MaximumLength(200).Matches("^[A-Za-z0-9_\\-=]+$");
        RuleFor(x => x.Auth).NotEmpty().MaximumLength(100).Matches("^[A-Za-z0-9_\\-=]+$");
    }
}

internal sealed class SubscribePushCommandHandler(
    IPushSender sender,
    IPushSubscriptionRepository subscriptions,
    IUserContext user,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<SubscribePushCommand, PushSettingsDto>
{
    public async Task<Result<PushSettingsDto>> Handle(SubscribePushCommand command, CancellationToken cancellationToken)
    {
        if (!sender.IsConfigured)
            return Result<PushSettingsDto>.Fail(PushErrors.NotConfigured);

        var existing = await subscriptions.GetByEndpointAsync(command.Endpoint, cancellationToken);
        if (existing is not null)
        {
            existing.Reassign(user.UserId, command.P256dh, command.Auth);
        }
        else
        {
            // Cihaz sınırı: en eski abonelik bırakılır (eski tarayıcılar çoğu zaman artık yoktur).
            var mine = await subscriptions.GetForUserAsync(user.UserId, cancellationToken);
            foreach (var old in mine.OrderBy(s => s.CreatedAt).Take(Math.Max(0, mine.Count - PushSubscription.MaxPerUser + 1)))
                await subscriptions.DeleteAsync(old, cancellationToken);

            await subscriptions.AddAsync(PushSubscription.Create(
                user.UserId, command.Endpoint, command.P256dh, command.Auth, clock.GetUtcNow().UtcDateTime), cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<PushSettingsDto>.Ok(new PushSettingsDto(
            true, sender.PublicKey, (await subscriptions.GetForUserAsync(user.UserId, cancellationToken)).Count));
    }
}

public sealed record UnsubscribePushCommand(string Endpoint) : ICommand;

internal sealed class UnsubscribePushCommandHandler(
    IPushSubscriptionRepository subscriptions,
    IUserContext user,
    IUnitOfWork unitOfWork) : ICommandHandler<UnsubscribePushCommand>
{
    public async Task<Result> Handle(UnsubscribePushCommand command, CancellationToken cancellationToken)
    {
        var existing = await subscriptions.GetByEndpointAsync(command.Endpoint, cancellationToken);
        // Başkasının aboneliği ya da bilinmeyen adres için de aynı yanıt.
        if (existing is not null && existing.UserId == user.UserId)
        {
            await subscriptions.DeleteAsync(existing, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok();
    }
}

// ── Deneme bildirimi ──────────────────────────────────────────────────────────

public sealed record SendTestPushCommand : ICommand<int>;

internal sealed class SendTestPushCommandHandler(PushNotifier notifier, IUserContext user, IUnitOfWork unitOfWork)
    : ICommandHandler<SendTestPushCommand, int>
{
    public async Task<Result<int>> Handle(SendTestPushCommand command, CancellationToken cancellationToken)
    {
        if (!notifier.IsConfigured)
            return Result<int>.Fail(PushErrors.NotConfigured);

        var delivered = await notifier.SendToUserAsync(user.UserId,
            new PushNotification("LifeQuest", Text.Of("Bildirimler çalışıyor. Hatırlatmanı seçtiğin saatte göndereceğiz.", "Notifications are working. We'll send your reminder at the time you chose."), "/today", "test"),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return delivered > 0 ? Result<int>.Ok(delivered) : Result<int>.Fail(PushErrors.NoDevices);
    }
}
