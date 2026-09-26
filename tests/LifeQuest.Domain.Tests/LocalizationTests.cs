using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Localization;
using LifeQuest.Domain.Quests;

namespace LifeQuest.Domain.Tests;

public sealed class LocalizationTests
{
    [Theory]
    [InlineData("en-GB", "en")]
    [InlineData("EN", "en")]
    [InlineData("tr-TR", "tr")]
    [InlineData("de", "tr")]
    [InlineData(null, "tr")]
    public void Languages_normalize_to_tr_or_en(string? input, string expected) => Assert.Equal(expected, Language.Normalize(input));

    [Fact]
    public void Text_and_errors_follow_the_current_language_and_scopes_restore_it()
    {
        using (Language.Use("tr"))
        {
            Assert.Equal("Quest bulunamadı.", QuestErrors.NotFound.Message);
            using (Language.Use("en"))
            {
                Assert.Equal("Quest not found.", QuestErrors.NotFound.Message);
                Assert.Equal("en", Language.Current);
                Assert.Contains("at most 5 active quests", QuestErrors.TooManyActiveQuests(5).Message);
            }
            Assert.Equal("tr", Language.Current);
            Assert.Equal("QUEST_NOT_FOUND", QuestErrors.NotFound.Code);
        }
    }

    [Fact]
    public void Localized_text_falls_back_to_turkish_when_english_is_missing()
    {
        var text = LocalizedText.WithFallback("Kahve", null);
        Assert.Equal("Kahve", text.In("en"));
        using (Language.Use("en"))
            Assert.Equal("Coffee", new LocalizedText("Kahve", "Coffee").Current);
    }

    [Fact]
    public void Account_language_defaults_to_turkish_and_accepts_only_supported_values()
    {
        var account = UserAccount.Register("a@b.com", "hash", "Ayşe", 1990, DateTime.UtcNow).Data!;
        Assert.Equal("tr", account.Language);
        Assert.True(account.SetLanguage("en").Succeeded);
        Assert.Equal("en", account.Language);
        Assert.False(account.SetLanguage("de").Succeeded);
        Assert.Equal("en", UserAccount.Register("b@b.com", "hash", "Bob", 1990, DateTime.UtcNow, "en-US").Data!.Language);
    }
}
