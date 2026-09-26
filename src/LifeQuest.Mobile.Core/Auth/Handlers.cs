using System.Net;
using System.Net.Http.Headers;
using LifeQuest.Mobile.Core.Localization;

namespace LifeQuest.Mobile.Core.Auth;

/// <summary>Her isteğe dil ve istemci türü başlıkları eklenir (native istemci refresh token'ı gövdede alır).</summary>
public sealed class ClientHeadersHandler : DelegatingHandler
{
    public const string ClientHeader = "X-LifeQuest-Client";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.AcceptLanguage.Clear();
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(Lang.Code));
        request.Headers.Remove(ClientHeader);
        request.Headers.Add(ClientHeader, "native");
        return base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// Bearer token ekler; 401'de (web'deki interceptor ile aynı kural) <c>ACCOUNT_SUSPENDED</c> ise oturumu kapatır,
/// diğer 401'lerde (<c>TOKEN_STALE</c> dahil) bir kez yeniler ve isteği tekrarlar.
/// </summary>
public sealed class AuthHandler(SessionStore session) : DelegatingHandler
{
    private static readonly string[] Anonymous = ["auth/login", "auth/register", "auth/refresh"];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        if (Anonymous.Any(a => path.EndsWith(a, StringComparison.OrdinalIgnoreCase)))
            return await base.SendAsync(request, cancellationToken);

        if (session.AccessToken is null && session.HasSession)
            await session.RefreshAsync();

        if (request.Content is not null)
            await request.Content.LoadIntoBufferAsync(cancellationToken);

        var used = session.AccessToken;
        Authorize(request, used);
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || !session.HasSession)
            return response;

        var body = response.Content is null ? "" : await response.Content.ReadAsStringAsync(cancellationToken);
        if (body.Contains("ACCOUNT_SUSPENDED", StringComparison.Ordinal))
        {
            await session.EndAsync(SessionEndReason.Suspended);
            return Rebuffer(response, body);
        }

        if (!await session.RefreshAsync(used))
            return Rebuffer(response, body);

        response.Dispose();
        using var retry = await CloneAsync(request, cancellationToken);
        Authorize(retry, session.AccessToken);
        return await base.SendAsync(retry, cancellationToken);
    }

    private static void Authorize(HttpRequestMessage request, string? token)
        => request.Headers.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);

    private static HttpResponseMessage Rebuffer(HttpResponseMessage response, string body)
    {
        var mediaType = response.Content?.Headers.ContentType;
        response.Content = new StringContent(body);
        if (mediaType is not null) response.Content.Headers.ContentType = mediaType;
        return response;
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content is not null)
        {
            clone.Content = new ByteArrayContent(await request.Content.ReadAsByteArrayAsync(cancellationToken));
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
