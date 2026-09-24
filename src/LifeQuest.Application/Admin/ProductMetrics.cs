using FluentValidation;
using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Quests;

namespace LifeQuest.Application.Admin;

/// <summary>Ürün metrikleri için ham sayımlar (Infrastructure'da SQL aggregate'leriyle okunur).</summary>
public sealed record ProductMetricsSnapshot(
    int ActiveUsers,
    int Offered,
    int Accepted,
    int Completed,
    int MeaningfulCompletions,
    int NewCategoryCompletions,
    int ExplorationOffered,
    int ExplorationAccepted,
    double? AverageRating,
    IReadOnlyDictionary<SkipReason, int> SkipReasons,
    IReadOnlyDictionary<LifeCategory, int> CompletionsByCategory);

public interface IProductMetricsReader
{
    Task<ProductMetricsSnapshot> ReadAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}

public sealed record FunnelDto(int Offered, int Accepted, int Completed, double AcceptanceRate, double CompletionRate);

public sealed record ShareDto<T>(T Key, int Count, double Share);

/// <param name="NorthStar">
/// Haftalık anlamlı tamamlanmış gerçek deneyim / aktif kullanıcı. Anlamlı = tamamlanmış ve (puansız veya puan ≥ 4).
/// Periyot 7 günden farklıysa haftalık değere ölçeklenir.
/// </param>
public sealed record ProductMetricsDto(
    DateTime From,
    DateTime To,
    int ActiveUsers,
    int MeaningfulCompletions,
    double NorthStar,
    FunnelDto Funnel,
    double NewCategoryDiscoveryRate,
    double ExplorationAcceptanceRate,
    double? AverageRating,
    IReadOnlyList<ShareDto<SkipReason>> SkipReasons,
    IReadOnlyList<ShareDto<LifeCategory>> CompletionsByCategory);

public sealed record GetProductMetricsQuery(int Days = 7) : IQuery<ProductMetricsDto>;

public sealed class GetProductMetricsQueryValidator : AbstractValidator<GetProductMetricsQuery>
{
    public GetProductMetricsQueryValidator() => RuleFor(x => x.Days).InclusiveBetween(1, 90);
}

internal sealed class GetProductMetricsQueryHandler(IProductMetricsReader reader, TimeProvider clock)
    : IQueryHandler<GetProductMetricsQuery, ProductMetricsDto>
{
    public async Task<Result<ProductMetricsDto>> Handle(GetProductMetricsQuery query, CancellationToken cancellationToken)
    {
        var to = clock.GetUtcNow().UtcDateTime;
        var from = to.AddDays(-query.Days);
        var s = await reader.ReadAsync(from, to, cancellationToken);

        return Result<ProductMetricsDto>.Ok(ProductMetricsCalculator.Build(s, from, to, query.Days));
    }
}

public static class ProductMetricsCalculator
{
    public static ProductMetricsDto Build(ProductMetricsSnapshot s, DateTime from, DateTime to, int days)
    {
        var weeklyFactor = 7d / days;
        var northStar = s.ActiveUsers == 0 ? 0 : s.MeaningfulCompletions / (double)s.ActiveUsers * weeklyFactor;

        return new ProductMetricsDto(
            from, to, s.ActiveUsers, s.MeaningfulCompletions, Round(northStar),
            new FunnelDto(s.Offered, s.Accepted, s.Completed, Rate(s.Accepted, s.Offered), Rate(s.Completed, s.Accepted)),
            Rate(s.NewCategoryCompletions, s.Completed),
            Rate(s.ExplorationAccepted, s.ExplorationOffered),
            s.AverageRating is { } rating ? Round(rating) : null,
            Shares(s.SkipReasons),
            Shares(s.CompletionsByCategory));
    }

    private static List<ShareDto<T>> Shares<T>(IReadOnlyDictionary<T, int> counts) where T : notnull
    {
        var total = counts.Values.Sum();
        return counts
            .OrderByDescending(x => x.Value)
            .Select(x => new ShareDto<T>(x.Key, x.Value, Rate(x.Value, total)))
            .ToList();
    }

    private static double Rate(int part, int whole) => whole == 0 ? 0 : Round(part / (double)whole);

    private static double Round(double value) => Math.Round(value, 3);
}
