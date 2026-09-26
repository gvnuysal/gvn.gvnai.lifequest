using LifeQuest.Domain.Common;
using LifeQuest.Domain.Localization;

namespace LifeQuest.Domain.Notifications;

/// <summary>
/// Haftalık özet metni, iki dilde. Sağlıklı oyunlaştırma ilkesi: sakin geçen bir hafta suçlanmaz, kayıp korkusu ve
/// "serini kaybettin" dili kullanılmaz; yalnızca yaşananlar kutlanır ve nazik bir davet yapılır.
/// </summary>
public static class WeeklySummaryComposer
{
    public static readonly LocalizedText Title = new("Haftalık özetin", "Your weekly summary");

    public static (LocalizedText Title, LocalizedText Message) Compose(WeeklyStats stats)
    {
        if (stats.CompletedCount > 0)
        {
            var tr = new List<string>
            {
                $"Bu hafta {stats.CompletedCount} gerçek deneyim yaşadın ve {stats.XpEarned} XP kazandın."
            };
            var en = new List<string>
            {
                stats.CompletedCount == 1
                    ? $"This week you had 1 real-life experience and earned {stats.XpEarned} XP."
                    : $"This week you had {stats.CompletedCount} real-life experiences and earned {stats.XpEarned} XP."
            };

            if (stats.NewCategories.Count > 0)
            {
                var names = stats.NewCategories.Select(c => c.LocalizedName()).ToList();
                tr.Add($"{Join(names.Select(n => n.Tr), " ve ")} alanında ilk adımını attın.");
                en.Add($"You took your first step in {Join(names.Select(n => n.En), " and ")}.");
            }

            if (stats.TopCategory is { } top)
            {
                var name = top.LocalizedName();
                tr.Add($"En çok {name.Tr} alanında vakit geçirdin.");
                en.Add($"You spent the most time on {name.En}.");
            }

            return (Title, new LocalizedText(string.Join(" ", tr), string.Join(" ", en)));
        }

        if (stats.OpenAcceptedCount > 0)
            return (Title, new LocalizedText(
                $"Devam eden {stats.OpenAcceptedCount} görevin var. Acele yok; uygun bir anda göz atabilirsin.",
                stats.OpenAcceptedCount == 1
                    ? "You have 1 quest in progress. No rush; take a look whenever it suits you."
                    : $"You have {stats.OpenAcceptedCount} quests in progress. No rush; take a look whenever it suits you."));

        return (Title, new LocalizedText(
            "Bu hafta sakin geçti, bu da güzel. Yeni haftada küçük bir deneyime ne dersin?",
            "It was a quiet week, and that's fine too. How about a small experience in the new week?"));
    }

    private static string Join(IEnumerable<string> items, string lastSeparator)
    {
        var list = items.ToList();
        return list.Count <= 1 ? string.Join("", list) : string.Join(", ", list[..^1]) + lastSeparator + list[^1];
    }
}
