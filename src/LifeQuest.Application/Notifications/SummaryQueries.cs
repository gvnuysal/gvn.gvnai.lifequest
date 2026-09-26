using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Notifications;
using LifeQuest.Domain.Localization;

namespace LifeQuest.Application.Notifications;

public sealed record WeeklySummaryDto(
    Guid Id, DateOnly WeekStart, string Title, string Message, int CompletedCount, int XpEarned,
    IReadOnlyList<LifeCategory> NewCategories, LifeCategory? TopCategory, DateTime CreatedAt);

/// <summary>Okunmamış en son özet; yoksa <c>null</c> veri ile başarılı döner.</summary>
public sealed record GetLatestSummaryQuery : IQuery<WeeklySummaryDto?>;

internal sealed class GetLatestSummaryQueryHandler(IWeeklySummaryRepository summaries, IUserContext user)
    : IQueryHandler<GetLatestSummaryQuery, WeeklySummaryDto?>
{
    public async Task<Result<WeeklySummaryDto?>> Handle(GetLatestSummaryQuery query, CancellationToken cancellationToken)
    {
        var summary = await summaries.GetLatestUnreadAsync(user.UserId, cancellationToken);
        if (summary is null)
            return Result<WeeklySummaryDto?>.Ok(null);

        // Metin saklanan istatistiklerden isteğin dilinde kurulur.
        var (title, message) = summary.Text;
        return Result<WeeklySummaryDto?>.Ok(new WeeklySummaryDto(
            summary.Id, summary.WeekStart, title.Current, message.Current, summary.CompletedCount, summary.XpEarned,
            summary.NewCategories, summary.TopCategory, summary.CreatedAt));
    }
}

public sealed record MarkSummaryReadCommand(Guid SummaryId) : ICommand;

internal sealed class MarkSummaryReadCommandHandler(
    IWeeklySummaryRepository summaries,
    IUserContext user,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<MarkSummaryReadCommand>
{
    public async Task<Result> Handle(MarkSummaryReadCommand command, CancellationToken cancellationToken)
    {
        var summary = await summaries.GetForUserAsync(command.SummaryId, user.UserId, cancellationToken);
        if (summary is null)
            return Result.Fail(Gvn.GvnFramework.Core.Results.Error.NotFound("SUMMARY_NOT_FOUND", Text.Of("Özet bulunamadı.", "Summary not found.")));

        summary.MarkRead(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
