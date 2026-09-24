using LifeQuest.Application.Diagnostics;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LifeQuest.Application.Narration;

/// <summary>
/// Anlatıcıyı zaman aşımı + guardrail ile çağırır. Hata, zaman aşımı veya guard ihlalinde template metnine
/// döner; LifeQuest'in çekirdek fonksiyonu harici modele bağımlı olmaz.
/// </summary>
public sealed class QuestNarrationService(
    IQuestNarrator narrator,
    IOptions<NarrationOptions> options,
    LifeQuestMetrics metrics,
    ILogger<QuestNarrationService> logger)
{
    public async Task<QuestText> NarrateAsync(
        QuestCandidate candidate, IReadOnlyList<string> topicNames, CancellationToken cancellationToken)
    {
        var fallback = new QuestText(candidate.Title, candidate.Description, NarrationSource.Template);
        if (narrator.Source == NarrationSource.Template)
            return fallback;

        var request = NarrationRequest.From(candidate, topicNames);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.TimeoutMilliseconds);

        try
        {
            var narration = await narrator.NarrateAsync(request, timeout.Token);
            var violations = NarrationGuard.Validate(narration, request);
            if (violations.Count == 0)
                return new QuestText(narration.Title.Trim(), narration.Description.Trim(), narrator.Source);

            logger.LogWarning("Narration for {TemplateCode} rejected by guard: {Violations}",
                candidate.Code, string.Join(", ", violations));
            metrics.NarrationFallback("guard");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Narration for {TemplateCode} timed out", candidate.Code);
            metrics.NarrationFallback("timeout");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Narration for {TemplateCode} failed", candidate.Code);
            metrics.NarrationFallback("error");
        }

        return fallback;
    }
}
