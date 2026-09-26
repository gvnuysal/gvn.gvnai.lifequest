using System.Net;
using System.Net.Http.Json;
using LifeQuest.Mobile.Core.Api;

namespace LifeQuest.Mobile.Core.Auth;

/// <summary>Cihazın güvenli deposu (iOS Keychain / Android Keystore); testlerde bellek içi.</summary>
public interface ISecureStore
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    void Remove(string key);
}

public enum SessionEndReason
{
    SignedOut,
    Expired,
    Suspended
}

/// <summary>
/// Oturum durumu. Access token yalnızca bellekte, refresh token güvenli depoda durur. Sunucu her yenilemede token'ı
/// döndürür ve eski token'ın yeniden kullanımında tüm oturum ailesini iptal eder; bu yüzden yenileme tek uçuşludur:
/// aynı anda gelen 401'ler tek bir yenilemeyi bekler.
/// </summary>
public sealed class SessionStore(ISecureStore store, HttpClient authClient)
{
    internal const string RefreshTokenKey = "lq.refresh";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private Task<bool>? _refreshing;
    private string? _refreshToken;
    private bool _loaded;

    public string? AccessToken { get; private set; }
    public Guid? UserId { get; private set; }

    public bool HasSession => _refreshToken is not null;

    /// <summary>Oturum sona erdi (çıkış, süresi doldu, hesap askıya alındı); uygulama giriş ekranına döner.</summary>
    public event EventHandler<SessionEndReason>? Ended;

    /// <summary>Uygulama açılışında: saklı refresh token varsa oturumu sessizce geri yükler.</summary>
    public async Task<bool> RestoreAsync()
    {
        await LoadAsync();
        return HasSession && await RefreshAsync();
    }

    public async Task StartAsync(AuthSession session)
    {
        if (string.IsNullOrWhiteSpace(session.RefreshToken))
            throw new InvalidOperationException("Native oturum yanıtında refresh token yok.");

        AccessToken = session.AccessToken;
        UserId = session.UserId;
        _refreshToken = session.RefreshToken;
        _loaded = true;
        await store.SetAsync(RefreshTokenKey, session.RefreshToken);
    }

    /// <summary>
    /// Access token'ı yeniler. <paramref name="staleAccessToken"/> verilirse ve o arada başka bir istek zaten
    /// yenilediyse ağa çıkmadan başarılı döner.
    /// </summary>
    public async Task<bool> RefreshAsync(string? staleAccessToken = null)
    {
        Task<bool> task;
        await _gate.WaitAsync();
        try
        {
            if (staleAccessToken is not null && AccessToken is not null && AccessToken != staleAccessToken)
                return true;
            task = _refreshing ??= RefreshCoreAsync();
        }
        finally
        {
            _gate.Release();
        }

        try
        {
            return await task;
        }
        finally
        {
            await _gate.WaitAsync();
            if (_refreshing == task) _refreshing = null;
            _gate.Release();
        }
    }

    private async Task<bool> RefreshCoreAsync()
    {
        await LoadAsync();
        if (_refreshToken is null)
            return false;

        HttpResponseMessage response;
        try
        {
            response = await authClient.PostAsJsonAsync("auth/refresh", new { refreshToken = _refreshToken }, Json.Options);
        }
        catch (HttpRequestException)
        {
            // Ağ yok: oturum korunur, istek başarısız sayılır.
            throw ApiException.Network();
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await EndAsync(SessionEndReason.Expired);
                return false;
            }

            if (!response.IsSuccessStatusCode)
                throw ApiErrorParser.Parse(response.StatusCode, await response.Content.ReadAsStringAsync());

            var session = await response.Content.ReadFromJsonAsync<AuthSession>(Json.Options);
            await StartAsync(session!);
            return true;
        }
    }

    /// <summary>Yerel oturumu siler ve <see cref="Ended"/> olayını yayınlar.</summary>
    public Task EndAsync(SessionEndReason reason)
    {
        var had = HasSession || AccessToken is not null;
        AccessToken = null;
        UserId = null;
        _refreshToken = null;
        _loaded = true;
        store.Remove(RefreshTokenKey);
        if (had) Ended?.Invoke(this, reason);
        return Task.CompletedTask;
    }

    internal async Task<string?> CurrentRefreshTokenAsync()
    {
        await LoadAsync();
        return _refreshToken;
    }

    private async Task LoadAsync()
    {
        if (_loaded) return;
        _refreshToken = await store.GetAsync(RefreshTokenKey);
        _loaded = true;
    }
}
