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

    public static Error InvalidTransition(QuestStatus from, string action) =>
        Error.Conflict("INVALID_QUEST_TRANSITION", $"{from} durumundaki bir quest için '{action}' yapılamaz.");

    public static Error TooManyActiveQuests(int max) =>
        TooManyActive with { Message = string.Format(TooManyActive.Message, max) };
}
