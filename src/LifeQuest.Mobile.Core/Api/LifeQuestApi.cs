using System.Net;
using System.Net.Http.Json;
using LifeQuest.Mobile.Core.Auth;

namespace LifeQuest.Mobile.Core.Api;

/// <summary>
/// API v1 istemcisi (web: core/api/api-clients.ts). Başarısız yanıtlar <see cref="ApiException"/> olarak fırlatılır.
/// </summary>
public sealed class LifeQuestApi(HttpClient http)
{
    // ── Oturum ──────────────────────────────────────────────────────────────
    public Task<AuthSession> RegisterAsync(RegisterRequest body) => Post<AuthSession>("auth/register", body);
    public Task<AuthSession> LoginAsync(LoginRequest body) => Post<AuthSession>("auth/login", body);
    public Task LogoutAsync(string? refreshToken) => Send(HttpMethod.Post, "auth/logout", new { refreshToken });

    // ── Katalog, profil, onboarding ─────────────────────────────────────────
    public Task<IReadOnlyList<Interest>> InterestsAsync() => Get<IReadOnlyList<Interest>>("catalog/interests");
    public Task<Profile> ProfileAsync() => Get<Profile>("profile");
    public Task<Profile> CompleteOnboardingAsync(OnboardingRequest body) => Send<Profile>(HttpMethod.Put, "profile/onboarding", body);
    public Task<Profile> UpdatePreferencesAsync(PreferencesRequest body) => Send<Profile>(HttpMethod.Patch, "profile/preferences", body);
    public Task<Profile> SetInterestsAsync(IReadOnlyList<InterestSelection> interests)
        => Send<Profile>(HttpMethod.Put, "profile/interests", new { interests });
    public Task DeleteAccountAsync(string password) => Send(HttpMethod.Delete, "profile", new { password });
    public Task<DownloadedFile> ExportDataAsync() => Download("profile/export", "lifequest-data.json");
    public Task<IReadOnlyList<StarterCard>> StarterCardsAsync() => Get<IReadOnlyList<StarterCard>>("onboarding/starter-cards");

    // ── Görevler ────────────────────────────────────────────────────────────
    public Task<QuestList> TodayAsync() => Get<QuestList>("quests/today");
    public Task<QuestList> SuggestAsync(SuggestRequest body) => Post<QuestList>("quests/suggestions", body);
    public Task<IReadOnlyList<Quest>> ActiveAsync() => Get<IReadOnlyList<Quest>>("quests/active");
    public Task<PagedResult<Quest>> HistoryAsync(int pageNumber, int pageSize, QuestStatus? status)
        => Get<PagedResult<Quest>>($"quests/history?pageNumber={pageNumber}&pageSize={pageSize}" + (status is null ? "" : $"&status={status}"));
    public Task<QuestDetail> QuestAsync(Guid id) => Get<QuestDetail>($"quests/{id}");
    public Task<Quest> AcceptAsync(Guid id) => Post<Quest>($"quests/{id}/accept", null);
    public Task<QuestCompletion> CompleteAsync(Guid id) => Post<QuestCompletion>($"quests/{id}/complete", null);
    public Task<Quest> SkipAsync(Guid id, SkipReason reason) => Post<Quest>($"quests/{id}/skip", new { reason });
    public Task<QuestFeedbackResult> FeedbackAsync(Guid id, int? rating, FeedbackPreference? preference)
        => Post<QuestFeedbackResult>($"quests/{id}/feedback", new FeedbackBody(rating, preference));
    public Task<SavedQuest> SaveAsync(Guid id) => Post<SavedQuest>($"quests/{id}/save", null);
    /// <param name="plannedAtLocal">Kullanıcının yerel saati; <c>null</c> planı kaldırır.</param>
    public Task<Quest> PlanAsync(Guid id, DateTime? plannedAtLocal)
        => Send<Quest>(HttpMethod.Put, $"quests/{id}/plan", new PlanBody(plannedAtLocal is { } d ? DateTime.SpecifyKind(d, DateTimeKind.Unspecified) : null));
    public Task<DownloadedFile> CalendarAsync(Guid id) => Download($"quests/{id}/calendar.ics", "lifequest.ics");

    // ── Kaydedilenler, fikirler, ilerleme, özet, parti ──────────────────────
    public Task<IReadOnlyList<SavedQuest>> SavedAsync() => Get<IReadOnlyList<SavedQuest>>("saved");
    public Task RemoveSavedAsync(Guid templateId) => Send(HttpMethod.Delete, $"saved/{templateId}", null);
    public Task<Quest> StartSavedAsync(Guid templateId) => Post<Quest>($"saved/{templateId}/start", null);

    public Task<MyIdea> SubmitIdeaAsync(IdeaRequest idea) => Post<MyIdea>("ideas", idea);
    public Task<IReadOnlyList<MyIdea>> MyIdeasAsync() => Get<IReadOnlyList<MyIdea>>("ideas/mine");
    public Task WithdrawIdeaAsync(Guid id) => Send(HttpMethod.Delete, $"ideas/{id}", null);

    public Task<Progress> ProgressAsync() => Get<Progress>("progress");
    public Task<IReadOnlyList<Achievement>> AchievementsAsync() => Get<IReadOnlyList<Achievement>>("achievements");

    /// <summary>Okunmamış özet yoksa <c>null</c> (API 204 döner).</summary>
    public Task<WeeklySummary?> LatestSummaryAsync() => Get<WeeklySummary?>("summaries/latest");
    public Task MarkSummaryReadAsync(Guid id) => Send(HttpMethod.Post, $"summaries/{id}/read", null);

    public Task<Party> CreatePartyAsync(Guid questId) => Post<Party>($"quests/{questId}/party", null);
    public Task<PartyInvite> PartyInviteAsync(string code) => Get<PartyInvite>($"parties/{Uri.EscapeDataString(code)}");
    public Task<PartyInvite> JoinPartyAsync(string code) => Post<PartyInvite>($"parties/{Uri.EscapeDataString(code)}/join", null);
    public Task LeavePartyAsync(string code) => Send(HttpMethod.Delete, $"parties/{Uri.EscapeDataString(code)}/members/me", null);

    private sealed record FeedbackBody(int? Rating, FeedbackPreference? Preference);

    private sealed record PlanBody(DateTime? PlannedAtLocal);

    // ── Taşıma ──────────────────────────────────────────────────────────────
    private Task<T> Get<T>(string path) => Send<T>(HttpMethod.Get, path, null);

    private Task<T> Post<T>(string path, object? body) => Send<T>(HttpMethod.Post, path, body);

    private async Task Send(HttpMethod method, string path, object? body)
    {
        using var response = await SendRaw(method, path, body);
    }

    private async Task<T> Send<T>(HttpMethod method, string path, object? body)
    {
        using var response = await SendRaw(method, path, body);
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            return default!;
        return (await response.Content.ReadFromJsonAsync<T>(Json.Options))!;
    }

    private async Task<DownloadedFile> Download(string path, string fallbackName)
    {
        using var response = await SendRaw(HttpMethod.Get, path, null);
        var name = response.Content.Headers.ContentDisposition?.FileNameStar?.Trim('"')
                   ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                   ?? fallbackName;
        var type = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        return new DownloadedFile(name, type, await response.Content.ReadAsByteArrayAsync());
    }

    private async Task<HttpResponseMessage> SendRaw(HttpMethod method, string path, object? body)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: Json.Options);
        else if (method == HttpMethod.Post || method == HttpMethod.Put)
            request.Content = JsonContent.Create<object?>(null, options: Json.Options);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            throw ApiException.Network();
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw ApiException.Network();
        }

        if (response.IsSuccessStatusCode)
            return response;

        using (response)
            throw ApiErrorParser.Parse(response.StatusCode, await response.Content.ReadAsStringAsync());
    }
}

/// <summary>Giriş, kayıt ve çıkış: API çağrısı ve yerel oturumun birlikte yönetimi.</summary>
public sealed class AuthService(LifeQuestApi api, SessionStore session)
{
    public async Task LoginAsync(string email, string password)
        => await session.StartAsync(await api.LoginAsync(new LoginRequest(email.Trim(), password)));

    public async Task RegisterAsync(RegisterRequest request)
        => await session.StartAsync(await api.RegisterAsync(request));

    /// <summary>Sunucuda oturum ailesi kapatılır; ağ hatasında bile yerel oturum silinir.</summary>
    public async Task LogoutAsync()
    {
        try
        {
            await api.LogoutAsync(await session.CurrentRefreshTokenAsync());
        }
        catch (ApiException)
        {
            // Sunucuya ulaşılamasa da cihazdaki oturum kapanır; token 30 gün sonra kendiliğinden geçersizleşir.
        }

        await session.EndAsync(SessionEndReason.SignedOut);
    }
}

/// <summary>API adresinden istemci zincirini kurar: başlıklar → oturum → (platform) ağ katmanı.</summary>
public static class ApiClientFactory
{
    public static (LifeQuestApi Api, SessionStore Session, AuthService Auth) Create(
        Uri apiBaseUrl, ISecureStore store, Func<HttpMessageHandler>? transport = null)
    {
        var root = new Uri(apiBaseUrl.ToString().TrimEnd('/') + "/api/v1/");
        HttpMessageHandler Transport() => transport?.Invoke() ?? new HttpClientHandler { UseCookies = false };

        var authClient = new HttpClient(new ClientHeadersHandler { InnerHandler = Transport() }) { BaseAddress = root };
        var session = new SessionStore(store, authClient);
        var client = new HttpClient(new ClientHeadersHandler { InnerHandler = new AuthHandler(session) { InnerHandler = Transport() } })
        {
            BaseAddress = root,
            Timeout = TimeSpan.FromSeconds(30)
        };
        var api = new LifeQuestApi(client);
        return (api, session, new AuthService(api, session));
    }
}
