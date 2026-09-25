using System.Security.Cryptography;
using Gvn.GvnFramework.Core.Guarding;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Aggregates;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;

namespace LifeQuest.Domain.Experiments;

/// <summary>
/// Öneri ağırlıkları için A/B deneyi: Kontrol grubu üretim ağırlıklarıyla, Deneme grubu bunların üzerine
/// <see cref="TreatmentOverrides"/> uygulanmış ağırlıklarla öneri alır. Aynı anda tek deney çalışır.
/// Kullanıcı ataması saklanmaz; kullanıcı ve deney kimliğinden deterministik olarak hesaplanır.
/// </summary>
public sealed class Experiment : AggregateRoot
{
    public const double MinShare = 0.1;
    public const double MaxShare = 0.9;

    public string Name { get; private set; } = default!;
    public string Hypothesis { get; private set; } = default!;
    public ExperimentStatus Status { get; private set; } = ExperimentStatus.Draft;
    public Dictionary<string, double> TreatmentOverrides { get; private set; } = [];

    /// <summary>Deneme grubuna düşen kullanıcı payı.</summary>
    public double TreatmentShare { get; private set; } = 0.5;

    public DateTime? StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public ExperimentOutcome Outcome { get; private set; } = ExperimentOutcome.None;
    /// <summary>Deneyi oluşturan admin'in e-postası (denetim kaydıyla aynı biçim).</summary>
    public string CreatedByEmail { get; private set; } = default!;

    private Experiment() { }

    public static Result<Experiment> Create(
        string name, string hypothesis, IReadOnlyDictionary<string, double> overrides, double treatmentShare, string createdBy)
    {
        var errors = RecommendationWeightCatalog.Validate(overrides);
        if (errors.Count > 0)
            return Result<Experiment>.Fail(errors.Select(e => Error.Validation($"TreatmentOverrides.{e.Key}", e.Value)).ToArray());
        if (overrides.Count == 0)
            return Result<Experiment>.Fail(ExperimentErrors.NoChanges);
        if (treatmentShare is < MinShare or > MaxShare)
            return Result<Experiment>.Fail(ExperimentErrors.InvalidShare);

        return Result<Experiment>.Ok(new Experiment
        {
            Name = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim(),
            Hypothesis = Guard.NotNullOrWhiteSpace(hypothesis, nameof(hypothesis)).Trim(),
            TreatmentOverrides = overrides.ToDictionary(o => o.Key, o => RecommendationWeightCatalog.Get(o.Key).Normalize(o.Value)),
            TreatmentShare = Math.Round(treatmentShare, 2),
            CreatedByEmail = createdBy
        });
    }

    public Result Start(DateTime nowUtc)
    {
        if (Status != ExperimentStatus.Draft)
            return Result.Fail(ExperimentErrors.InvalidTransition(Status, "başlat"));

        Status = ExperimentStatus.Running;
        StartedAt = nowUtc;
        return Result.Ok();
    }

    public Result Stop(DateTime nowUtc)
    {
        if (Status != ExperimentStatus.Running)
            return Result.Fail(ExperimentErrors.InvalidTransition(Status, "durdur"));

        Status = ExperimentStatus.Stopped;
        EndedAt = nowUtc;
        return Result.Ok();
    }

    /// <summary>Deneme ağırlıkları üretime alınacak; çağıran taraf ayarları günceller.</summary>
    public Result Adopt() => Conclude(ExperimentOutcome.Adopted);

    public Result Discard() => Conclude(ExperimentOutcome.Discarded);

    private Result Conclude(ExperimentOutcome outcome)
    {
        if (Status != ExperimentStatus.Stopped || Outcome != ExperimentOutcome.None)
            return Result.Fail(ExperimentErrors.MustStopFirst);

        Outcome = outcome;
        return Result.Ok();
    }

    public ExperimentVariant VariantFor(Guid userId) => ExperimentAssignment.VariantFor(userId, Id, TreatmentShare);
}

public static class ExperimentAssignment
{
    /// <summary>
    /// Kullanıcı ve deney kimliğinin SHA-256 özetinden [0,1) aralığında bir sayı üretir: aynı kullanıcı deney boyunca
    /// hep aynı gruptadır, farklı deneylerde gruplar birbirinden bağımsızdır.
    /// </summary>
    public static ExperimentVariant VariantFor(Guid userId, Guid experimentId, double treatmentShare)
    {
        Span<byte> input = stackalloc byte[32];
        userId.TryWriteBytes(input[..16]);
        experimentId.TryWriteBytes(input[16..]);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(input, hash);

        var bucket = BitConverter.ToUInt32(hash[..4]) / (double)uint.MaxValue;
        return bucket < treatmentShare ? ExperimentVariant.Treatment : ExperimentVariant.Control;
    }
}

public enum ExperimentStatus
{
    Draft = 1,
    Running = 2,
    Stopped = 3
}

public enum ExperimentOutcome
{
    None = 0,
    Adopted = 1,
    Discarded = 2
}

public static class ExperimentErrors
{
    public static readonly Error NotFound = Error.NotFound("EXPERIMENT_NOT_FOUND", "Deney bulunamadı.");

    public static readonly Error AnotherRunning =
        Error.Conflict("EXPERIMENT_ALREADY_RUNNING", "Aynı anda yalnızca bir deney çalışabilir. Önce çalışan deneyi durdur.");

    public static readonly Error NoChanges =
        Error.Validation("TreatmentOverrides", "Deneme grubunda en az bir ağırlık farklı olmalı.");

    public static readonly Error InvalidShare =
        Error.Validation("TreatmentShare", $"Deneme payı %{Experiment.MinShare * 100:0} ile %{Experiment.MaxShare * 100:0} arasında olmalı.");

    public static readonly Error MustStopFirst =
        Error.Conflict("EXPERIMENT_NOT_STOPPED", "Sonuç ancak durdurulmuş ve henüz karara bağlanmamış bir deney için verilebilir.");

    public static Error InvalidTransition(ExperimentStatus from, string action) =>
        Error.Conflict("EXPERIMENT_INVALID_TRANSITION", $"{from} durumundaki deney için '{action}' yapılamaz.");
}

public interface IExperimentRepository : IRepository<Experiment>
{
    Task<Experiment?> GetRunningAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Experiment>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
}
