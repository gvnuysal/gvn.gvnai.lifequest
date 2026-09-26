using System.Text.Json;
using LifeQuest.Domain.Admin;
using LifeQuest.Domain.Experiments;
using LifeQuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifeQuest.Infrastructure.Admin;

public sealed class ExperimentsOptions
{
    public const string SectionName = "Experiments";

    /// <summary>
    /// Açılışta başlatılacak hazır deney (<see cref="ExperimentPresets"/> anahtarı). Test ortamında gerçek trafik
    /// toplamaya başlamak için. Aynı değişiklikle bir deney daha önce oluşturulduysa (durdurulmuş olsa da) tekrar açılmaz.
    /// </summary>
    public string? AutoStartPreset { get; set; }
}

/// <summary>Hazır deneyi açılışta taslak olarak oluşturup başlatır; denetim kaydına "system" aktörüyle yazılır.</summary>
public static class ExperimentAutoStart
{
    public const string SystemActor = "system";

    public static async Task RunAsync(
        LifeQuestDbContext db, string? presetKey, DateTime nowUtc, ILogger logger, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(presetKey))
            return;

        var preset = ExperimentPresets.Find(presetKey.Trim());
        if (preset is null)
        {
            logger.LogWarning("Experiments:AutoStartPreset '{Preset}' is not a known preset; skipping", presetKey);
            return;
        }

        var existing = await db.Experiments.AsNoTracking().ToListAsync(cancellationToken);
        if (existing.Any(e => e.Outcome != ExperimentOutcome.Discarded && SameOverrides(e.TreatmentOverrides, preset.Overrides)))
            return;

        if (existing.Any(e => e.Status == ExperimentStatus.Running))
        {
            logger.LogInformation("Another experiment is running; preset {Preset} will not be auto-started", preset.Key);
            return;
        }

        var created = Experiment.Create(preset.Name, preset.Hypothesis, preset.Overrides, preset.TreatmentShare, SystemActor);
        if (!created.Succeeded)
        {
            logger.LogWarning("Preset {Preset} is invalid: {Errors}", preset.Key, string.Join(", ", created.Errors.Select(e => e.Code)));
            return;
        }

        var experiment = created.Data!;
        experiment.Start(nowUtc);
        db.Experiments.Add(experiment);

        var details = JsonSerializer.Serialize(new { preset = preset.Key, experiment.TreatmentShare, overrides = experiment.TreatmentOverrides });
        const string reason = "Açılışta otomatik (Experiments:AutoStartPreset)";
        db.AdminAuditEntries.Add(AdminAuditEntry.Create(Guid.Empty, SystemActor, AdminAction.ExperimentCreated,
            AdminTargetType.Experiment, experiment.Id, experiment.Name, reason, details, nowUtc));
        db.AdminAuditEntries.Add(AdminAuditEntry.Create(Guid.Empty, SystemActor, AdminAction.ExperimentStarted,
            AdminTargetType.Experiment, experiment.Id, experiment.Name, reason, null, nowUtc));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Experiment {ExperimentId} started from preset {Preset}", experiment.Id, preset.Key);
        }
        catch (DbUpdateException ex)
        {
            // Aynı anda açılan ikinci örnek de başlatmayı denediyse tekil "çalışan deney" indeksi ikisinden birini reddeder.
            logger.LogInformation(ex, "Preset {Preset} was started by another instance", preset.Key);
        }
    }

    private static bool SameOverrides(IReadOnlyDictionary<string, double> a, IReadOnlyDictionary<string, double> b)
        => a.Count == b.Count && a.All(o => b.TryGetValue(o.Key, out var v) && Math.Abs(v - o.Value) < 1e-9);
}
