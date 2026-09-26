namespace LifeQuest.Application.Abstractions;

/// <summary>Bildirime tıklanınca açılacak uygulama içi yol (ör. "/today").</summary>
public sealed record PushNotification(string Title, string Body, string Url, string Tag);

public sealed record PushTarget(string Endpoint, string P256dh, string Auth);

public enum PushDelivery
{
    Sent = 1,
    /// <summary>Push servisi aboneliğin artık geçerli olmadığını söyledi (404/410): silinmeli.</summary>
    Expired = 2,
    Failed = 3
}

/// <summary>Web Push (VAPID) gönderimi. Anahtarlar yapılandırılmadıysa <see cref="IsConfigured"/> false döner.</summary>
public interface IPushSender
{
    bool IsConfigured { get; }

    /// <summary>Tarayıcının abonelik için kullandığı VAPID public key (base64url).</summary>
    string? PublicKey { get; }

    Task<PushDelivery> SendAsync(PushTarget target, PushNotification notification, CancellationToken cancellationToken);
}
