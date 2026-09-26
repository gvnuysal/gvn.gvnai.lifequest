using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Application.Admin;
using LifeQuest.Domain.Admin;
using LifeQuest.Domain.Experiments;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;
using Microsoft.Extensions.Options;

namespace LifeQuest.Application.Experiments;

/// <summary>Deneye ait görevlerden kullanıcı başına istatistikler (Infrastructure'da SQL ile okunur).</summary>
public interface IExperimentMetricsReader
{
    Task<IReadOnlyDictionary<ExperimentVariant, IReadOnlyList<UserExperimentStats>>> ReadAsync(
        Guid experimentId, CancellationToken cancellationToken = default);
}

public sealed record OverrideDto(string Key, string Label, double ControlValue, double TreatmentValue);

public sealed record ExperimentDto(
    Guid Id,
    string Name,
    string Hypothesis,
    ExperimentStatus Status,
    ExperimentOutcome Outcome,
    double TreatmentShare,
    IReadOnlyList<OverrideDto> Overrides,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? StartedAt,
    DateTime? EndedAt);

public sealed record ExperimentResultsDto(
    double Weeks,
    VariantResult Control,
    VariantResult Treatment,
    NorthStarComparison NorthStar,
    ExperimentVerdict Verdict,
    int MinUsersPerVariant,
    bool GuardrailBreached,
    double GuardrailMaxIncrease);

public sealed record ExperimentDetailDto(ExperimentDto Experiment, ExperimentResultsDto? Results);

internal static class ExperimentMapping
{
    public static ExperimentDto ToDto(this Experiment e, RecommendationWeights production) => new(
        e.Id, e.Name, e.Hypothesis, e.Status, e.Outcome, e.TreatmentShare,
        e.TreatmentOverrides
            .Select(o =>
            {
                var field = RecommendationWeightCatalog.Get(o.Key);
                return new OverrideDto(o.Key, field.Label, field.Get(production), o.Value);
            })
            .OrderBy(o => o.Label)
            .ToList(),
        e.CreatedAt, e.CreatedByEmail, e.StartedAt, e.EndedAt);
}

// ── Sorgular ──────────────────────────────────────────────────────────────────

public sealed record ListExperimentsQuery : IQuery<IReadOnlyList<ExperimentDto>>;

internal sealed class ListExperimentsQueryHandler(IExperimentRepository experiments, IRecommendationWeightsProvider weights)
    : IQueryHandler<ListExperimentsQuery, IReadOnlyList<ExperimentDto>>
{
    public async Task<Result<IReadOnlyList<ExperimentDto>>> Handle(ListExperimentsQuery query, CancellationToken cancellationToken)
    {
        var production = await weights.GetAsync(cancellationToken);
        return Result<IReadOnlyList<ExperimentDto>>.Ok(
            (await experiments.GetAllOrderedAsync(cancellationToken)).Select(e => e.ToDto(production)).ToList());
    }
}

public sealed record GetExperimentQuery(Guid ExperimentId) : IQuery<ExperimentDetailDto>;

internal sealed class GetExperimentQueryHandler(
    IExperimentRepository experiments,
    IExperimentMetricsReader reader,
    IRecommendationWeightsProvider weights,
    TimeProvider clock) : IQueryHandler<GetExperimentQuery, ExperimentDetailDto>
{
    public async Task<Result<ExperimentDetailDto>> Handle(GetExperimentQuery query, CancellationToken cancellationToken)
    {
        var experiment = await experiments.GetByIdAsync(query.ExperimentId, cancellationToken);
        if (experiment is null)
            return Result<ExperimentDetailDto>.Fail(ExperimentErrors.NotFound);

        var dto = experiment.ToDto(await weights.GetAsync(cancellationToken));
        if (experiment.StartedAt is not { } started)
            return Result<ExperimentDetailDto>.Ok(new ExperimentDetailDto(dto, null));

        var end = experiment.EndedAt ?? clock.GetUtcNow().UtcDateTime;
        var weeks = Math.Round((end - started).TotalDays / 7, 2);
        var stats = await reader.ReadAsync(experiment.Id, cancellationToken);
        var control = ExperimentStatistics.Summarize(stats.GetValueOrDefault(ExperimentVariant.Control) ?? [], weeks);
        var treatment = ExperimentStatistics.Summarize(stats.GetValueOrDefault(ExperimentVariant.Treatment) ?? [], weeks);
        var comparison = ExperimentStatistics.Compare(control, treatment);

        return Result<ExperimentDetailDto>.Ok(new ExperimentDetailDto(dto, new ExperimentResultsDto(
            weeks, control, treatment, comparison, ExperimentStatistics.Verdict(control, treatment, comparison),
            ExperimentStatistics.MinUsersPerVariant,
            ExperimentStatistics.GuardrailBreached(control, treatment),
            ExperimentStatistics.GuardrailMaxNotInterestedIncrease)));
    }
}

/// <param name="ExistingExperimentId">Aynı değişikliği deneyen, kapatılmamış son deney (varsa yeniden oluşturulmaz).</param>
public sealed record ExperimentPresetDto(
    string Key,
    string Name,
    string Hypothesis,
    double TreatmentShare,
    string Source,
    IReadOnlyList<OverrideDto> Overrides,
    Guid? ExistingExperimentId,
    ExperimentStatus? ExistingStatus);

public sealed record ListExperimentPresetsQuery : IQuery<IReadOnlyList<ExperimentPresetDto>>;

internal sealed class ListExperimentPresetsQueryHandler(IExperimentRepository experiments, IRecommendationWeightsProvider weights)
    : IQueryHandler<ListExperimentPresetsQuery, IReadOnlyList<ExperimentPresetDto>>
{
    public async Task<Result<IReadOnlyList<ExperimentPresetDto>>> Handle(ListExperimentPresetsQuery query, CancellationToken cancellationToken)
    {
        var production = await weights.GetAsync(cancellationToken);
        var existing = (await experiments.GetAllOrderedAsync(cancellationToken))
            .Where(e => e.Outcome != ExperimentOutcome.Discarded)
            .ToList();

        return Result<IReadOnlyList<ExperimentPresetDto>>.Ok(ExperimentPresets.All.Select(preset =>
        {
            var match = existing.FirstOrDefault(e => SameOverrides(e.TreatmentOverrides, preset.Overrides));
            var overrides = preset.Overrides.Select(o =>
            {
                var field = RecommendationWeightCatalog.Get(o.Key);
                return new OverrideDto(o.Key, field.Label, field.Get(production), o.Value);
            }).ToList();
            return new ExperimentPresetDto(preset.Key, preset.Name, preset.Hypothesis, preset.TreatmentShare, preset.Source,
                overrides, match?.Id, match?.Status);
        }).ToList());
    }

    private static bool SameOverrides(IReadOnlyDictionary<string, double> a, IReadOnlyDictionary<string, double> b)
        => a.Count == b.Count && a.All(o => b.TryGetValue(o.Key, out var v) && Math.Abs(v - o.Value) < 1e-9);
}

// ── Komutlar ──────────────────────────────────────────────────────────────────

public sealed record CreateExperimentCommand(
    string Name, string Hypothesis, IReadOnlyDictionary<string, double> TreatmentOverrides, double TreatmentShare)
    : ICommand<ExperimentDto>;

public sealed class CreateExperimentCommandValidator : AbstractValidator<CreateExperimentCommand>
{
    public CreateExperimentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Hypothesis).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TreatmentOverrides).NotNull();
    }
}

internal sealed class CreateExperimentCommandHandler(
    IExperimentRepository experiments,
    IRecommendationWeightsProvider weights,
    AdminAuditWriter audit,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateExperimentCommand, ExperimentDto>
{
    public async Task<Result<ExperimentDto>> Handle(CreateExperimentCommand command, CancellationToken cancellationToken)
    {
        var production = await weights.GetAsync(cancellationToken);

        // Üretimdeki değerle aynı olan "değişiklik" deney değildir; yalnızca farklı olanlar saklanır.
        var changed = command.TreatmentOverrides
            .Where(o => !RecommendationWeightCatalog.TryGet(o.Key, out var f) || Math.Abs(f.Get(production) - o.Value) > 1e-9)
            .ToDictionary(o => o.Key, o => o.Value);

        var actor = await audit.GetActorAsync(cancellationToken);
        var created = Experiment.Create(command.Name, command.Hypothesis, changed, command.TreatmentShare, actor.Email);
        if (!created.Succeeded)
            return Result<ExperimentDto>.Fail(created.Errors);

        var experiment = created.Data!;
        await experiments.AddAsync(experiment, cancellationToken);
        await audit.RecordAsync(AdminAction.ExperimentCreated, AdminTargetType.Experiment, experiment.Id, experiment.Name,
            command.Hypothesis, new { experiment.TreatmentShare, overrides = experiment.TreatmentOverrides }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ExperimentDto>.Ok(experiment.ToDto(production));
    }
}

public enum ExperimentCommand
{
    Start = 1,
    Stop = 2,
    Adopt = 3,
    Discard = 4
}

/// <summary>Deneyi başlatır, durdurur veya sonucunu uygular. Kazanan uygulanınca deneme ağırlıkları üretime yazılır.</summary>
public sealed record ChangeExperimentCommand(Guid ExperimentId, ExperimentCommand Action, string? Reason) : ICommand<ExperimentDto>;

public sealed class ChangeExperimentCommandValidator : AbstractValidator<ChangeExperimentCommand>
{
    public ChangeExperimentCommandValidator()
    {
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

internal sealed class ChangeExperimentCommandHandler(
    IExperimentRepository experiments,
    IRecommendationSettingsRepository settingsRepository,
    IRecommendationWeightsProvider weights,
    IOptions<RecommendationWeights> defaults,
    AdminAuditWriter audit,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<ChangeExperimentCommand, ExperimentDto>
{
    public async Task<Result<ExperimentDto>> Handle(ChangeExperimentCommand command, CancellationToken cancellationToken)
    {
        var experiment = await experiments.GetByIdAsync(command.ExperimentId, cancellationToken);
        if (experiment is null)
            return Result<ExperimentDto>.Fail(ExperimentErrors.NotFound);

        var now = clock.GetUtcNow().UtcDateTime;
        Result result;
        AdminAction action;
        object? details = null;

        switch (command.Action)
        {
            case ExperimentCommand.Start:
                if (await experiments.GetRunningAsync(cancellationToken) is not null)
                    return Result<ExperimentDto>.Fail(ExperimentErrors.AnotherRunning);
                result = experiment.Start(now);
                action = AdminAction.ExperimentStarted;
                break;
            case ExperimentCommand.Stop:
                result = experiment.Stop(now);
                action = AdminAction.ExperimentStopped;
                break;
            case ExperimentCommand.Adopt:
                result = experiment.Adopt();
                action = AdminAction.ExperimentAdopted;
                if (result.Succeeded)
                    details = await AdoptAsync(experiment, now, cancellationToken);
                break;
            default:
                result = experiment.Discard();
                action = AdminAction.ExperimentDiscarded;
                break;
        }

        if (!result.Succeeded)
            return Result<ExperimentDto>.Fail(result.Errors);

        await audit.RecordAsync(action, AdminTargetType.Experiment, experiment.Id, experiment.Name, command.Reason, details,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await weights.InvalidateAsync(cancellationToken);

        return Result<ExperimentDto>.Ok(experiment.ToDto(await weights.GetAsync(cancellationToken)));
    }

    /// <summary>Deneme override'larını üretim ayarlarına yazar (Öneri ayarları ekranındaki gibi, geçmişiyle).</summary>
    private async Task<IReadOnlyList<WeightChange>> AdoptAsync(Experiment experiment, DateTime now, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        if (settings is null)
        {
            settings = RecommendationSettings.CreateEmpty();
            await settingsRepository.AddAsync(settings, cancellationToken);
        }

        var actor = await audit.GetActorAsync(cancellationToken);
        return settings.Update(experiment.TreatmentOverrides, defaults.Value, actor.Email, now);
    }
}
