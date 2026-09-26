using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Auth;
using LifeQuest.Mobile.Core.Localization;

namespace LifeQuest.Api.IntegrationTests;

/// <summary>
/// Mobil uygulamanın çekirdek istemcisi gerçek API'ye karşı: modellerin sunucu DTO'larıyla uyumu ve native oturum
/// akışı (refresh token gövdede, güvenli depoda).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class MobileClientContractTests(LifeQuestApiFactory factory)
{
    private sealed class MemoryStore : ISecureStore
    {
        public Dictionary<string, string> Values { get; } = new();
        public Task<string?> GetAsync(string key) => Task.FromResult(Values.GetValueOrDefault(key));
        public Task SetAsync(string key, string value) { Values[key] = value; return Task.CompletedTask; }
        public void Remove(string key) => Values.Remove(key);
    }

    [Fact]
    public async Task Mobile_client_runs_the_core_flow_in_both_languages()
    {
        var previous = Lang.Current;
        Lang.Set(AppLanguage.En);
        try
        {
            var store = new MemoryStore();
            var (api, session, auth) = ApiClientFactory.Create(factory.Server.BaseAddress, store, () => factory.Server.CreateHandler());

            await auth.RegisterAsync(new RegisterRequest($"mobile-{Guid.NewGuid():N}@example.com", LifeQuestApiFactory.Password, "Alex", 1990, "en"));
            Assert.True(session.HasSession);
            Assert.NotEmpty(store.Values);

            var interests = await api.InterestsAsync();
            Assert.Contains(interests, i => i.Name == "Café Culture");
            var cards = await api.StarterCardsAsync();
            Assert.NotEmpty(cards);

            var profile = await api.CompleteOnboardingAsync(new OnboardingRequest(
                [LifeCategory.Culture, LifeCategory.Fitness],
                interests.Take(3).Select(i => new InterestSelection(i.Code, 0.9)).ToList(),
                300, CostBand.Low, DiscoveryRadius.Explore, "Istanbul", "Europe/Istanbul", PhysicalEffort.Moderate,
                [new StarterReaction(cards[0].Code, StarterReactionType.Like)]));
            Assert.True(profile.OnboardingCompleted);
            Assert.Equal("en", profile.Language);

            var today = await api.TodayAsync();
            Assert.NotEmpty(today.Quests);
            var quest = today.Quests[0];
            Assert.Matches("^(Suggested because|Since|One of the best)", quest.Explanation);

            var detail = await api.QuestAsync(quest.Id);
            Assert.Equal(quest.Id, detail.Quest.Id);
            Assert.True(detail.Score.Total > 0);

            await api.AcceptAsync(quest.Id);
            Assert.Contains(await api.ActiveAsync(), q => q.Id == quest.Id);
            var completion = await api.CompleteAsync(quest.Id);
            Assert.Equal(QuestStatus.Completed, completion.Quest.Status);
            Assert.Contains(completion.NewAchievements, a => a.Title == "First Step");
            await api.FeedbackAsync(quest.Id, 5, FeedbackPreference.MoreLikeThis);

            var progress = await api.ProgressAsync();
            Assert.Equal(1, progress.TotalCompleted);
            Assert.Equal(6, progress.Categories.Count);
            var history = await api.HistoryAsync(1, 20, QuestStatus.Completed);
            Assert.Single(history.Items);

            // Dil değişince aynı görev Türkçe döner; profil de güncellenir.
            Lang.Set(AppLanguage.Tr);
            profile = await api.UpdatePreferencesAsync(new PreferencesRequest { Language = "tr" });
            Assert.Equal("tr", profile.Language);
            Assert.Contains((await api.ProgressAsync()).Categories, c => c.DisplayName == "Hareket");

            // Refresh gövdeyle döner, token rotasyonlu; çıkış depoyu temizler.
            var before = store.Values.Values.Single();
            Assert.True(await session.RefreshAsync());
            Assert.NotEqual(before, store.Values.Values.Single());
            await auth.LogoutAsync();
            Assert.Empty(store.Values);
            Assert.False(session.HasSession);
        }
        finally
        {
            Lang.Set(previous);
        }
    }

    [Fact]
    public async Task Validation_errors_reach_the_mobile_form_fields()
    {
        var (_, _, auth) = ApiClientFactory.Create(factory.Server.BaseAddress, new MemoryStore(), () => factory.Server.CreateHandler());

        var ex = await Assert.ThrowsAsync<ApiException>(() => auth.RegisterAsync(new RegisterRequest("not-an-email", "short", "A", 1990, "tr")));

        var fields = ex.FieldErrors();
        Assert.Contains("email", fields.Keys);
        Assert.Contains("password", fields.Keys);
    }
}
