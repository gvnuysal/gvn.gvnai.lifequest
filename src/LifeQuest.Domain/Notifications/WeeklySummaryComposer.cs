using LifeQuest.Domain.Common;

namespace LifeQuest.Domain.Notifications;

/// <summary>
/// Haftalık özet metni. Sağlıklı oyunlaştırma ilkesi: sakin geçen bir hafta suçlanmaz, kayıp korkusu ve
/// "serini kaybettin" dili kullanılmaz; yalnızca yaşananlar kutlanır ve nazik bir davet yapılır.
/// </summary>
public static class WeeklySummaryComposer
{
    public const string Title = "Haftalık özetin";

    public static (string Title, string Message) Compose(WeeklyStats stats)
    {
        if (stats.CompletedCount > 0)
        {
            var parts = new List<string>
            {
                stats.CompletedCount == 1
                    ? $"Bu hafta 1 gerçek deneyim yaşadın ve {stats.XpEarned} XP kazandın."
                    : $"Bu hafta {stats.CompletedCount} gerçek deneyim yaşadın ve {stats.XpEarned} XP kazandın."
            };

            if (stats.NewCategories.Count > 0)
                parts.Add($"{JoinTurkish(stats.NewCategories.Select(c => c.DisplayName()))} alanında ilk adımını attın.");

            if (stats.TopCategory is { } top)
                parts.Add($"En çok {top.DisplayName()} alanında vakit geçirdin.");

            return (Title, string.Join(" ", parts));
        }

        if (stats.OpenAcceptedCount > 0)
            return (Title,
                $"Devam eden {stats.OpenAcceptedCount} görevin var. Acele yok; uygun bir anda göz atabilirsin.");

        return (Title, "Bu hafta sakin geçti, bu da güzel. Yeni haftada küçük bir deneyime ne dersin?");
    }

    private static string JoinTurkish(IEnumerable<string> items)
    {
        var list = items.ToList();
        return list.Count <= 1 ? string.Join("", list) : string.Join(", ", list[..^1]) + " ve " + list[^1];
    }
}
