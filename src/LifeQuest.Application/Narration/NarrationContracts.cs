using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;

namespace LifeQuest.Application.Narration;

/// <summary>
/// AI Quest Master'a gidebilecek TEK girdi. Yapısal ve kişisel veri içermeyen alanlardan oluşur:
/// kullanıcı adı, e-posta, şehir, ilgi ağırlıkları veya kullanıcı serbest metni bu tipte bulunamaz
/// (prompt injection ve veri sızıntısına karşı yapısal garanti).
/// </summary>
public sealed record NarrationRequest(
    string TemplateCode,
    string BaseTitle,
    string BaseDescription,
    LifeCategory Category,
    QuestType Type,
    int MinMinutes,
    int MaxMinutes,
    CostBand Cost,
    IReadOnlyList<string> TopicNames)
{
    public static NarrationRequest From(QuestCandidate candidate, IReadOnlyList<string> topicNames) => new(
        candidate.Code, candidate.Title, candidate.Description, candidate.Category, candidate.Type,
        candidate.MinMinutes, candidate.MaxMinutes, candidate.Cost, topicNames);
}

/// <summary>Anlatıcının ürettiği metin. Kategori, süre, maliyet ve XP burada yoktur; LLM bunları değiştiremez.</summary>
public sealed record QuestNarration(string Title, string Description);

/// <summary>AI Quest Master portu. Varsayılan uygulama <see cref="TemplateQuestNarrator"/>'dır.</summary>
public interface IQuestNarrator
{
    NarrationSource Source { get; }

    Task<QuestNarration> NarrateAsync(NarrationRequest request, CancellationToken cancellationToken);
}

/// <summary>Deterministik varsayılan: template metnini aynen döner. AI servisi yokken çekirdek fonksiyon çalışır.</summary>
public sealed class TemplateQuestNarrator : IQuestNarrator
{
    public NarrationSource Source => NarrationSource.Template;

    public Task<QuestNarration> NarrateAsync(NarrationRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new QuestNarration(request.BaseTitle, request.BaseDescription));
}

public sealed class NarrationOptions
{
    public const string SectionName = "Narration";

    /// <summary>Anlatıcı bu süre içinde yanıt vermezse template metni kullanılır.</summary>
    public int TimeoutMilliseconds { get; set; } = 2000;
}
