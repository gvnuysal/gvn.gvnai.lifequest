using System.Collections.Concurrent;
using System.Net;
using System.Text;
using LifeQuest.Mobile.Core.Auth;
using LifeQuest.Mobile.Core.Localization;

// Dil statik bir durum; testler sırayla çalışır.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace LifeQuest.Mobile.Tests;

public sealed class MemorySecureStore : ISecureStore
{
    public ConcurrentDictionary<string, string> Values { get; } = new();

    public Task<string?> GetAsync(string key) => Task.FromResult(Values.TryGetValue(key, out var v) ? v : null);

    public Task SetAsync(string key, string value)
    {
        Values[key] = value;
        return Task.CompletedTask;
    }

    public void Remove(string key) => Values.TryRemove(key, out _);
}

/// <summary>Sahte ağ katmanı: her isteği kaydeder, yanıtı verilen fonksiyondan üretir.</summary>
public sealed class FakeTransport(Func<HttpRequestMessage, string, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    public ConcurrentQueue<(HttpRequestMessage Request, string Body)> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Enqueue((request, body));
        return await respond(request, body);
    }

    public static HttpResponseMessage JsonResponse(HttpStatusCode status, string json)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

public sealed class LanguageScope : IDisposable
{
    private readonly AppLanguage _previous = Lang.Current;

    public LanguageScope(AppLanguage language) => Lang.Set(language);

    public void Dispose() => Lang.Set(_previous);
}
