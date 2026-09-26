using Gvn.GvnFramework.Core.Results;

namespace LifeQuest.Domain.Quests;

public static class QuestErrors
{
    public static readonly Error NotFound =
        Error.NotFound("QUEST_NOT_FOUND", "Quest bulunamadı.");

    public static readonly Error Expired =
        Error.Conflict("QUEST_EXPIRED", "Bu quest'in süresi doldu.");

    public static readonly Error MustAcceptFirst =
        Error.Conflict("QUEST_NOT_ACCEPTED", "Tamamlamadan önce quest'i kabul etmelisin.");

    public static readonly Error TooManyActive =
        Error.Conflict("TOO_MANY_ACTIVE_QUESTS", "Aynı anda en fazla {0} aktif quest'in olabilir. Önce birini tamamla veya bırak.");

    public static readonly Error RatingRequiresCompletion =
        Error.Validation("RATING_REQUIRES_COMPLETION", "Değerlendirme yalnızca tamamlanan quest'ler için yapılabilir.");

    public static readonly Error InvalidRating =
        Error.Validation("INVALID_RATING", "Değerlendirme 1 ile 5 arasında olmalı.");

    public static readonly Error SuggestionLimitReached =
        Error.Conflict("SUGGESTION_LIMIT_REACHED", "Bugünkü bağlamsal öneri hakkını kullandın. Yarın tekrar dene.");

    public static readonly Error PlanRequiresAccepted =
        Error.Conflict("PLAN_REQUIRES_ACCEPTED", "Yalnızca kabul ettiğin bir quest'i planlayabilirsin.");

    public static readonly Error TemplateUnavailable =
        Error.Conflict("TEMPLATE_UNAVAILABLE", "Bu deneyim şu an sunulamıyor.");

    public static readonly Error SavedLimitReached =
        Error.Conflict("SAVED_LIMIT_REACHED", $"\"Sonra yaparım\" listende en fazla {SavedQuest.MaxPerUser} deneyim olabilir.");

    public static readonly Error SavedNotFound =
        Error.NotFound("SAVED_NOT_FOUND", "Bu deneyim listende yok.");

    public static Error PlanOutsideWindow(DateTime expiresAtUtc) =>
        Error.Validation("PlannedAt", $"Plan, şu andan sonra ve quest'in son günü ({expiresAtUtc:dd.MM.yyyy}) öncesinde olmalı.");

    /// <summary>Motorun uygunluk filtresi kodunu kullanıcıya anlaşılır bir sebebe çevirir.</summary>
    public static Error NotOfferableNow(string reason) => Error.Conflict("NOT_OFFERABLE_NOW", reason switch
    {
        "cooldown" => "Bu deneyimi yakın zamanda yaptın; biraz ara verip tekrar deneyebilirsin.",
        "already_open" => "Bu deneyim zaten aktif görevlerinde.",
        "over_budget" => "Bu deneyim şu anki bütçe tercihinin üstünde.",
        "requires_city" => "Bu deneyim için profilinde şehir bilgisi gerekiyor.",
        "effort_limit" => "Bu deneyim seçtiğin efor sınırının üstünde.",
        "rejected_by_user" => "Bu deneyim için yakın zamanda \"ilgimi çekmedi\" demiştin.",
        "outdoor_at_night" => "Bu açık hava deneyimi gece için uygun değil; gündüz başlatabilirsin.",
        "bad_weather" => "Hava şu an bu açık hava deneyimi için uygun değil; hava düzelince başlatabilirsin.",
        _ => "Bu deneyim şu an sana sunulamıyor."
    });

    public static Error InvalidTransition(QuestStatus from, string action) =>
        Error.Conflict("INVALID_QUEST_TRANSITION", $"{from} durumundaki bir quest için '{action}' yapılamaz.");

    public static Error TooManyActiveQuests(int max) =>
        TooManyActive with { Message = string.Format(TooManyActive.Message, max) };
}
