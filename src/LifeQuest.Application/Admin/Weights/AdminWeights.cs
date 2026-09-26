using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Admin;
using LifeQuest.Domain.Recommendations;
using Microsoft.Extensions.Options;
using LifeQuest.Domain.Localization;

namespace LifeQuest.Application.Admin.Weights;

public sealed record WeightFieldDto(
    string Key,
    string Label,
    WeightGroup Group,
    string Description,
    double Min,
    double Max,
    double Step,
    bool IsInteger,
    double Value,
    double DefaultValue,
    bool IsOverridden);

public sealed record RecommendationWeightsDto(
    int Revision,
    DateTime? UpdatedAt,
    string? UpdatedBy,
    IReadOnlyList<WeightFieldDto> Fields);

internal static class WeightMapping
{
    public static RecommendationWeightsDto ToDto(RecommendationSettings? settings, RecommendationWeights defaults)
    {
        var overrides = settings?.Overrides ?? [];
        var effective = RecommendationWeightCatalog.Apply(defaults, overrides);
        return new RecommendationWeightsDto(
            settings?.Revision ?? 0, settings?.UpdatedAt, settings?.UpdatedBy,
            RecommendationWeightCatalog.Fields
                .Select(f => new WeightFieldDto(f.Key, f.Label, f.Group, f.Description, f.Min, f.Max, f.Step, f.IsInteger,
                    f.Get(effective), f.Get(defaults), overrides.ContainsKey(f.Key)))
                .ToList());
    }
}

public sealed record GetRecommendationWeightsQuery : IQuery<RecommendationWeightsDto>;

internal sealed class GetRecommendationWeightsQueryHandler(
    IRecommendationSettingsRepository settings, IOptions<RecommendationWeights> defaults)
    : IQueryHandler<GetRecommendationWeightsQuery, RecommendationWeightsDto>
{
    public async Task<Result<RecommendationWeightsDto>> Handle(GetRecommendationWeightsQuery query, CancellationToken cancellationToken)
        => Result<RecommendationWeightsDto>.Ok(WeightMapping.ToDto(await settings.GetAsync(cancellationToken), defaults.Value));
}

/// <param name="Revision">İstemcinin gördüğü sürüm; arada başka bir admin kaydettiyse 409.</param>
/// <param name="Values">Yalnızca değişen anahtarlar gönderilebilir; diğerleri korunur.</param>
public sealed record UpdateRecommendationWeightsCommand(int Revision, IReadOnlyDictionary<string, double> Values, string Reason)
    : ICommand<RecommendationWeightsDto>;

public sealed class UpdateRecommendationWeightsCommandValidator : AbstractValidator<UpdateRecommendationWeightsCommand>
{
    public UpdateRecommendationWeightsCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Values).NotEmpty().WithMessage(_ => Text.Of("Değiştirilecek en az bir ağırlık gönderilmelidir.", "Send at least one weight to change."));
        RuleFor(x => x.Values).Custom((values, context) =>
        {
            if (values is null)
                return;
            foreach (var (key, message) in RecommendationWeightCatalog.Validate(values))
                context.AddFailure($"Values.{key}", message);
        });
    }
}

internal sealed class UpdateRecommendationWeightsCommandHandler(
    IRecommendationSettingsRepository settingsRepository,
    IRecommendationWeightsProvider provider,
    IOptions<RecommendationWeights> defaults,
    AdminAuditWriter audit,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<UpdateRecommendationWeightsCommand, RecommendationWeightsDto>
{
    public async Task<Result<RecommendationWeightsDto>> Handle(UpdateRecommendationWeightsCommand command, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        if ((settings?.Revision ?? 0) != command.Revision)
            return Result<RecommendationWeightsDto>.Fail(AdminErrors.StaleVersion);

        if (settings is null)
        {
            settings = RecommendationSettings.CreateEmpty();
            await settingsRepository.AddAsync(settings, cancellationToken);
        }

        var actor = await audit.GetActorAsync(cancellationToken);
        var changes = settings.Update(command.Values, defaults.Value, actor.Email, clock.GetUtcNow().UtcDateTime);
        if (changes.Count > 0)
        {
            await audit.RecordAsync(AdminAction.WeightsUpdated, AdminTargetType.RecommendationSettings, settings.Id,
                Text.Of("Öneri ağırlıkları", "Recommendation weights"), command.Reason, changes, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await provider.InvalidateAsync(cancellationToken);
        }

        return Result<RecommendationWeightsDto>.Ok(WeightMapping.ToDto(settings, defaults.Value));
    }
}

/// <param name="Keys"><c>null</c> ise tüm ağırlıklar konfigürasyon varsayılanına döner.</param>
public sealed record ResetRecommendationWeightsCommand(int Revision, IReadOnlyList<string>? Keys, string? Reason)
    : ICommand<RecommendationWeightsDto>;

public sealed class ResetRecommendationWeightsCommandValidator : AbstractValidator<ResetRecommendationWeightsCommand>
{
    public ResetRecommendationWeightsCommandValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleForEach(x => x.Keys).Must(k => RecommendationWeightCatalog.TryGet(k, out _)).WithMessage(_ => Text.Of("Bilinmeyen ağırlık: {PropertyValue}", "Unknown weight: {PropertyValue}"));
    }
}

internal sealed class ResetRecommendationWeightsCommandHandler(
    IRecommendationSettingsRepository settingsRepository,
    IRecommendationWeightsProvider provider,
    IOptions<RecommendationWeights> defaults,
    AdminAuditWriter audit,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<ResetRecommendationWeightsCommand, RecommendationWeightsDto>
{
    public async Task<Result<RecommendationWeightsDto>> Handle(ResetRecommendationWeightsCommand command, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        if ((settings?.Revision ?? 0) != command.Revision)
            return Result<RecommendationWeightsDto>.Fail(AdminErrors.StaleVersion);
        if (settings is null)
            return Result<RecommendationWeightsDto>.Ok(WeightMapping.ToDto(null, defaults.Value));

        var actor = await audit.GetActorAsync(cancellationToken);
        var changes = settings.Reset(command.Keys, defaults.Value, actor.Email, clock.GetUtcNow().UtcDateTime);
        if (changes.Count > 0)
        {
            await audit.RecordAsync(AdminAction.WeightsReset, AdminTargetType.RecommendationSettings, settings.Id,
                Text.Of("Öneri ağırlıkları", "Recommendation weights"), command.Reason, changes, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await provider.InvalidateAsync(cancellationToken);
        }

        return Result<RecommendationWeightsDto>.Ok(WeightMapping.ToDto(settings, defaults.Value));
    }
}
