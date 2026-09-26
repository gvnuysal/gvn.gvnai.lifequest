using System.Net;
using System.Text.Json;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using LifeQuest.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LifeQuest.Infrastructure.Notifications;

public sealed class PushOptions
{
    public const string SectionName = "Push";

    /// <summary>VAPID iletişim adresi (mailto: veya https:), push servisleri sorun olursa buraya yazar.</summary>
    public string Subject { get; set; } = "mailto:admin@lifequest.local";

    /// <summary>P-256 public key, base64url (65 bayt, sıkıştırılmamış). Boşsa push kapalıdır.</summary>
    public string? VapidPublicKey { get; set; }

    /// <summary>P-256 private key, base64url (32 bayt). Gizli: ortam değişkeni / user-secrets ile verilir.</summary>
    public string? VapidPrivateKey { get; set; }

    /// <summary>Kullanıcı çevrimdışıysa push servisinin bildirimi tutma süresi.</summary>
    public int TimeToLiveHours { get; set; } = 12;
}

/// <summary>
/// Web Push (RFC 8030/8291/8292) gönderimi. Yük Angular service worker'ın (SwPush) beklediği biçimdedir:
/// <c>{ notification: { title, body, icon, tag, data.onActionClick } }</c>; tıklanınca ilgili sayfa açılır.
/// </summary>
public sealed class WebPushSender : IPushSender
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly PushServiceClient? _client;
    private readonly PushOptions _options;
    private readonly ILogger<WebPushSender> _logger;

    public WebPushSender(HttpClient httpClient, IOptions<PushOptions> options, ILogger<WebPushSender> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.VapidPublicKey) || string.IsNullOrWhiteSpace(_options.VapidPrivateKey))
            return;

        _client = new PushServiceClient(httpClient)
        {
            DefaultAuthentication = new VapidAuthentication(_options.VapidPublicKey, _options.VapidPrivateKey)
            {
                Subject = _options.Subject
            },
            DefaultTimeToLive = _options.TimeToLiveHours * 3600
        };
    }

    public bool IsConfigured => _client is not null;

    public string? PublicKey => IsConfigured ? _options.VapidPublicKey : null;

    public async Task<PushDelivery> SendAsync(PushTarget target, PushNotification notification, CancellationToken cancellationToken)
    {
        if (_client is null)
            return PushDelivery.Failed;

        var subscription = new PushSubscription { Endpoint = target.Endpoint };
        subscription.SetKey(PushEncryptionKeyName.P256DH, target.P256dh);
        subscription.SetKey(PushEncryptionKeyName.Auth, target.Auth);

        var payload = JsonSerializer.Serialize(new
        {
            notification = new
            {
                title = notification.Title,
                body = notification.Body,
                icon = "icons/icon-192x192.png",
                badge = "icons/icon-72x72.png",
                tag = notification.Tag,
                data = new
                {
                    onActionClick = new
                    {
                        @default = new { operation = "navigateLastFocusedOrOpen", url = notification.Url }
                    }
                }
            }
        }, Json);

        try
        {
            // Topic: aynı türden bekleyen eski bildirim varsa push servisi yenisiyle değiştirir (ör. iki hatırlatma birikmez).
            await _client.RequestPushMessageDeliveryAsync(subscription,
                new PushMessage(payload) { Topic = notification.Tag, Urgency = PushMessageUrgency.Normal }, cancellationToken);
            return PushDelivery.Sent;
        }
        catch (PushServiceClientException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            return PushDelivery.Expired;
        }
        catch (Exception ex) when (ex is PushServiceClientException or HttpRequestException or TaskCanceledException
                                   && !cancellationToken.IsCancellationRequested)
        {
            // Adres kişisel veridir; loglanmaz. Yalnızca push servisinin kökü yazılır.
            _logger.LogWarning(ex, "Push delivery failed via {PushService}", new Uri(target.Endpoint).Host);
            return PushDelivery.Failed;
        }
    }
}
