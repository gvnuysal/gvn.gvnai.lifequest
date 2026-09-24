using System.Diagnostics.Metrics;
using LifeQuest.Application.Diagnostics;
using LifeQuest.Application.Narration;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LifeQuest.Application.Tests;

public sealed class NarrationGuardTests
{
    private static readonly NarrationRequest Request = new(
        "culture-museum-object", "Bir müzede tek bir esere odaklan",
        "Bir müzede tek bir eser seç, önünde on dakika geçir ve onun hikâyesini araştır.",
        LifeCategory.Culture, QuestType.Weekly, 45, 90, CostBand.Low, ["Müzeler", "Sanat"]);

    [Fact]
    public void Safe_rewrite_passes()
        => Assert.Empty(NarrationGuard.Validate(new QuestNarration(
            "Tek bir eserin hikâyesi",
            "Bu hafta bir müzede yalnızca bir esere odaklan; önünde biraz vakit geçir ve hikâyesini keşfet."), Request));

    [Theory]
    [InlineData("Müzeden sonra bir bira iç", "risky_content")]
    [InlineData("Konumunu paylaş ve arkadaşlarını çağır", "personal_data_request")]
    [InlineData("Bilet 150 ₺, hemen al", "money_or_reward")]
    [InlineData("Detaylar için www.ornek.com adresine git", "url")]
    [InlineData("Müzede 3 saat geçir ve 5 eser seç", "invented_number")]
    [InlineData("Bu görev sana ekstra XP kazandırır", "money_or_reward")]
    public void Unsafe_or_invented_content_is_rejected(string description, string violation)
    {
        var output = new QuestNarration("Müzede bir gün", description + " ve güzel bir deneyim yaşa.");
        Assert.Contains(violation, NarrationGuard.Validate(output, Request));
    }

    [Theory]
    [InlineData("Biraz vakit ayır ve sakin bir köşede eseri incele")]
    [InlineData("Yüksek rakımlı bir şehrin müzesini düşünerek eseri incele")]
    [InlineData("Arkadaşlarınla küçük bir bilgi yarışması yapar gibi eseri tartış")]
    public void Innocent_words_that_contain_risky_substrings_are_allowed(string description)
        => Assert.DoesNotContain("risky_content", NarrationGuard.Validate(new QuestNarration("Müzede bir an", description), Request));

    [Fact]
    public void Length_limits_are_enforced()
    {
        var violations = NarrationGuard.Validate(new QuestNarration(new string('a', 81), "kısa"), Request);
        Assert.Contains("title_length", violations);
        Assert.Contains("description_length", violations);
    }

    [Fact]
    public void Prompt_contains_only_structured_non_personal_fields()
    {
        var prompt = NarrationPromptBuilder.BuildUserPrompt(Request);

        Assert.Contains("Kategori: Kültür", prompt);
        Assert.Contains("Müzeler, Sanat", prompt);
        // NarrationRequest'te kullanıcıya ait alan yok: yapısal garanti.
        Assert.DoesNotContain(typeof(NarrationRequest).GetProperties().Select(p => p.Name),
            name => name is "UserId" or "Email" or "DisplayName" or "City" or "InterestWeights");
    }
}

public sealed class QuestNarrationServiceTests
{
    private static readonly QuestCandidate Candidate = new(
        Guid.NewGuid(), "learning-ten-pages", 1, "Yeni bir kitaptan 10 sayfa oku",
        "Başlamadığın bir kitabı aç ve ilk 10 sayfasını oku.", QuestType.Daily, Difficulty.Easy,
        LifeCategory.Learning, null, 15, 20, CostBand.Free, DayPart.Any, false, 0, 1, [Guid.NewGuid()]);

    private static QuestNarrationService Service(IQuestNarrator narrator, int timeoutMs = 200)
        => new(narrator, Options.Create(new NarrationOptions { TimeoutMilliseconds = timeoutMs }),
            new LifeQuestMetrics(new TestMeterFactory()), NullLogger<QuestNarrationService>.Instance);

    [Fact]
    public async Task Template_narrator_returns_template_text()
    {
        var text = await Service(new TemplateQuestNarrator()).NarrateAsync(Candidate, ["Okuma"], CancellationToken.None);
        Assert.Equal(NarrationSource.Template, text.Source);
        Assert.Equal(Candidate.Title, text.Title);
    }

    [Fact]
    public async Task Valid_ai_narration_is_used()
    {
        var narrator = new FakeNarrator(new QuestNarration("Yeni bir kitaba ilk adım", "Başlamadığın bir kitabı aç ve ilk 10 sayfasıyla tanış."));
        var text = await Service(narrator).NarrateAsync(Candidate, ["Okuma"], CancellationToken.None);

        Assert.Equal(NarrationSource.Ai, text.Source);
        Assert.Equal("Yeni bir kitaba ilk adım", text.Title);
    }

    [Fact]
    public async Task Guard_violation_falls_back_to_template()
    {
        var narrator = new FakeNarrator(new QuestNarration("Kitap ve şarap", "Bir kadeh şarapla 50 sayfa oku ve rahatla, harika olacak."));
        var text = await Service(narrator).NarrateAsync(Candidate, ["Okuma"], CancellationToken.None);

        Assert.Equal(NarrationSource.Template, text.Source);
        Assert.Equal(Candidate.Description, text.Description);
    }

    [Fact]
    public async Task Timeout_and_errors_fall_back_to_template()
    {
        var slow = await Service(new FakeNarrator(delay: TimeSpan.FromSeconds(5)), timeoutMs: 50)
            .NarrateAsync(Candidate, [], CancellationToken.None);
        var broken = await Service(new FakeNarrator(fail: true)).NarrateAsync(Candidate, [], CancellationToken.None);

        Assert.Equal(NarrationSource.Template, slow.Source);
        Assert.Equal(NarrationSource.Template, broken.Source);
    }

    private sealed class FakeNarrator(QuestNarration? output = null, TimeSpan? delay = null, bool fail = false) : IQuestNarrator
    {
        public NarrationSource Source => NarrationSource.Ai;

        public async Task<QuestNarration> NarrateAsync(NarrationRequest request, CancellationToken cancellationToken)
        {
            if (delay is { } d) await Task.Delay(d, cancellationToken);
            if (fail) throw new HttpRequestException("model unavailable");
            return output ?? new QuestNarration(request.BaseTitle, request.BaseDescription);
        }
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);
        public void Dispose() { }
    }
}
