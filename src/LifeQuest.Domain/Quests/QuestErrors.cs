using Gvn.GvnFramework.Core.Results;
using LifeQuest.Domain.Localization;

namespace LifeQuest.Domain.Quests;

/// <summary>Mesajlar her erişimde o anki dilde üretilir (özellik, alan değil).</summary>
public static class QuestErrors
{
    public static Error NotFound =>
        Error.NotFound("QUEST_NOT_FOUND", Text.Of("Quest bulunamadı.", "Quest not found."));

    public static Error Expired =>
        Error.Conflict("QUEST_EXPIRED", Text.Of("Bu quest'in süresi doldu.", "This quest has expired."));

    public static Error MustAcceptFirst =>
        Error.Conflict("QUEST_NOT_ACCEPTED", Text.Of("Tamamlamadan önce quest'i kabul etmelisin.", "Accept the quest before completing it."));

    public static Error RatingRequiresCompletion =>
        Error.Validation("RATING_REQUIRES_COMPLETION",
            Text.Of("Değerlendirme yalnızca tamamlanan quest'ler için yapılabilir.", "You can only rate completed quests."));

    public static Error InvalidRating =>
        Error.Validation("INVALID_RATING", Text.Of("Değerlendirme 1 ile 5 arasında olmalı.", "Rating must be between 1 and 5."));

    public static Error SuggestionLimitReached =>
        Error.Conflict("SUGGESTION_LIMIT_REACHED",
            Text.Of("Bugünkü bağlamsal öneri hakkını kullandın. Yarın tekrar dene.",
                "You've used today's on-demand suggestions. Try again tomorrow."));

    public static Error PlanRequiresAccepted =>
        Error.Conflict("PLAN_REQUIRES_ACCEPTED",
            Text.Of("Yalnızca kabul ettiğin bir quest'i planlayabilirsin.", "You can only plan a quest you've accepted."));

    public static Error TemplateUnavailable =>
        Error.Conflict("TEMPLATE_UNAVAILABLE", Text.Of("Bu deneyim şu an sunulamıyor.", "This experience isn't available right now."));

    public static Error SavedLimitReached =>
        Error.Conflict("SAVED_LIMIT_REACHED", Text.Of(
            $"\"Sonra yaparım\" listende en fazla {SavedQuest.MaxPerUser} deneyim olabilir.",
            $"Your \"Later\" list can hold at most {SavedQuest.MaxPerUser} experiences."));

    public static Error SavedNotFound =>
        Error.NotFound("SAVED_NOT_FOUND", Text.Of("Bu deneyim listende yok.", "This experience isn't on your list."));

    public static Error PlanOutsideWindow(DateTime expiresAtUtc) =>
        Error.Validation("PlannedAt", Text.Format(
            $"Plan, şu andan sonra ve quest'in son günü ({expiresAtUtc:d}) öncesinde olmalı.",
            $"The plan must be in the future and before the quest's last day ({expiresAtUtc:d})."));

    /// <summary>Motorun uygunluk filtresi kodunu kullanıcıya anlaşılır bir sebebe çevirir.</summary>
    public static Error NotOfferableNow(string reason) => Error.Conflict("NOT_OFFERABLE_NOW", reason switch
    {
        "cooldown" => Text.Of("Bu deneyimi yakın zamanda yaptın; biraz ara verip tekrar deneyebilirsin.",
            "You did this recently; give it a little time and try again."),
        "already_open" => Text.Of("Bu deneyim zaten aktif görevlerinde.", "This experience is already among your active quests."),
        "over_budget" => Text.Of("Bu deneyim şu anki bütçe tercihinin üstünde.", "This experience is above your current budget preference."),
        "requires_city" => Text.Of("Bu deneyim için profilinde şehir bilgisi gerekiyor.", "This experience needs a city in your profile."),
        "effort_limit" => Text.Of("Bu deneyim seçtiğin efor sınırının üstünde.", "This experience is above the effort limit you chose."),
        "rejected_by_user" => Text.Of("Bu deneyim için yakın zamanda \"ilgimi çekmedi\" demiştin.",
            "You recently marked this experience as \"not for me\"."),
        "outdoor_at_night" => Text.Of("Bu açık hava deneyimi gece için uygun değil; gündüz başlatabilirsin.",
            "This outdoor experience isn't suitable at night; you can start it during the day."),
        "bad_weather" => Text.Of("Hava şu an bu açık hava deneyimi için uygun değil; hava düzelince başlatabilirsin.",
            "The weather isn't right for this outdoor experience; you can start it once it clears up."),
        _ => Text.Of("Bu deneyim şu an sana sunulamıyor.", "This experience can't be offered to you right now.")
    });

    /// <param name="action">accept · complete · skip</param>
    public static Error InvalidTransition(QuestStatus from, string action) =>
        Error.Conflict("INVALID_QUEST_TRANSITION", Text.Of(
            $"{StatusTr(from)} durumdaki bir quest {action switch { "accept" => "kabul edilemez", "complete" => "tamamlanamaz", _ => "bırakılamaz" }}.",
            $"A quest that is {from.ToString().ToLowerInvariant()} can't be {action switch { "accept" => "accepted", "complete" => "completed", _ => "skipped" }}."));

    private static string StatusTr(QuestStatus status) => status switch
    {
        QuestStatus.Offered => "Önerilmiş",
        QuestStatus.Accepted => "Kabul edilmiş",
        QuestStatus.Completed => "Tamamlanmış",
        QuestStatus.Skipped => "Bırakılmış",
        _ => "Süresi dolmuş"
    };

    public static Error TooManyActiveQuests(int max) =>
        Error.Conflict("TOO_MANY_ACTIVE_QUESTS", Text.Of(
            $"Aynı anda en fazla {max} aktif quest'in olabilir. Önce birini tamamla veya bırak.",
            $"You can have at most {max} active quests at once. Complete or drop one first."));
}
