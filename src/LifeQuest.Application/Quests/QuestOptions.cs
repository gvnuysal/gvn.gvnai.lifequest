namespace LifeQuest.Application.Quests;

public sealed class QuestOptions
{
    public const string SectionName = "Quests";

    /// <summary>Günlük öneri sayısı (MVP: 3).</summary>
    public int DailyOfferCount { get; set; } = 3;

    /// <summary>Aynı anda kabul edilmiş quest sınırı; "biriktirip unutma" davranışını önler.</summary>
    public int MaxActiveQuests { get; set; } = 5;

    /// <summary>Günlük bağlamsal öneri ("bu akşam 2 saatim var") turu sınırı.</summary>
    public int MaxSuggestionRoundsPerDay { get; set; } = 3;

    public int SuggestionCount { get; set; } = 3;

    public int SuggestionTtlHours { get; set; } = 6;

    /// <summary>Öneri geçmişi penceresi (repetition, feedback, skip sinyalleri).</summary>
    public int HistoryWindowDays { get; set; } = 30;
}
