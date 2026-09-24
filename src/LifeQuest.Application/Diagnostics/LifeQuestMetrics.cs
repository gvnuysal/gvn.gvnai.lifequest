using System.Diagnostics.Metrics;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Quests;

namespace LifeQuest.Application.Diagnostics;

/// <summary>
/// Ürün funnel metrikleri (offered → accepted → completed, skip sebepleri, XP, öneri süresi).
/// OpenTelemetry veya dotnet-counters ile "LifeQuest" meter'ı üzerinden okunur.
/// </summary>
public sealed class LifeQuestMetrics
{
    public const string MeterName = "LifeQuest";

    private readonly Counter<long> _offered;
    private readonly Counter<long> _accepted;
    private readonly Counter<long> _completed;
    private readonly Counter<long> _skipped;
    private readonly Counter<long> _xpGranted;
    private readonly Counter<long> _emptyRecommendations;
    private readonly Histogram<double> _recommendationDuration;
    private readonly Counter<long> _narrationFallback;

    public LifeQuestMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _offered = meter.CreateCounter<long>("lifequest.quests.offered");
        _accepted = meter.CreateCounter<long>("lifequest.quests.accepted");
        _completed = meter.CreateCounter<long>("lifequest.quests.completed");
        _skipped = meter.CreateCounter<long>("lifequest.quests.skipped");
        _xpGranted = meter.CreateCounter<long>("lifequest.xp.granted");
        _emptyRecommendations = meter.CreateCounter<long>("lifequest.recommendations.empty");
        _recommendationDuration = meter.CreateHistogram<double>("lifequest.recommendations.duration", unit: "ms");
        _narrationFallback = meter.CreateCounter<long>("lifequest.narration.fallback");
    }

    public void Offered(QuestSource source, int count)
        => _offered.Add(count, new KeyValuePair<string, object?>("source", source.ToString()));

    public void Accepted(LifeCategory category)
        => _accepted.Add(1, new KeyValuePair<string, object?>("category", category.ToString()));

    public void Completed(LifeCategory category, int lifeXp)
    {
        var tag = new KeyValuePair<string, object?>("category", category.ToString());
        _completed.Add(1, tag);
        _xpGranted.Add(lifeXp, tag);
    }

    public void Skipped(SkipReason reason)
        => _skipped.Add(1, new KeyValuePair<string, object?>("reason", reason.ToString()));

    public void NarrationFallback(string reason)
        => _narrationFallback.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void RecommendationGenerated(double elapsedMs, bool empty)
    {
        _recommendationDuration.Record(elapsedMs);
        if (empty) _emptyRecommendations.Add(1);
    }
}
