namespace LifeQuest.Domain.Quests;

/// <summary>
/// Analizdeki Generated → Offered → Accepted → InProgress → Completed/Skipped/Expired akışı MVP'de sadeleştirildi:
/// üretim ve sunum aynı anda olduğu için Generated, ayrı bir "başladım" tetikleyicisi olmadığı için InProgress
/// durumları Offered ve Accepted içinde temsil edilir.
/// </summary>
public enum QuestStatus
{
    Offered = 1,
    Accepted = 2,
    Completed = 3,
    Skipped = 4,
    Expired = 5
}

public enum SkipReason
{
    NotInterested = 1,
    TooExpensive = 2,
    NoTime = 3,
    TooFar = 4,
    NotToday = 5,
    Other = 6
}

public enum QuestSource
{
    /// <summary>Günlük 3'lü öneri.</summary>
    Daily = 1,

    /// <summary>"Bu akşam 2 saatim var" gibi bağlamsal, kullanıcı tetiklemeli öneri.</summary>
    OnDemand = 2
}

public enum FeedbackPreference
{
    MoreLikeThis = 1,
    LessLikeThis = 2
}

public enum NarrationSource
{
    Template = 1,
    Ai = 2
}

/// <summary>Quest'in kullanıcıya gösterilecek metni. Kategori, süre, maliyet ve XP bu nesnede yoktur; hep template'ten gelir.</summary>
public sealed record QuestText(string Title, string Description, NarrationSource Source);
