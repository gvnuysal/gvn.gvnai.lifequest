using Gvn.GvnFramework.Application.Abstractions;
using Gvn.GvnFramework.Core.Results;
using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Domain.Quests;

namespace LifeQuest.Application.Quests;

/// <summary>Süresi geçen açık quest'leri kapatır (Hangfire job). İdempotent: tekrar çalışması güvenlidir.</summary>
public sealed record ExpireStaleQuestsCommand(int BatchSize = 500) : ICommand<int>;

internal sealed class ExpireStaleQuestsCommandHandler(
    IUserQuestRepository quests,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<ExpireStaleQuestsCommand, int>
{
    public async Task<Result<int>> Handle(ExpireStaleQuestsCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var total = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var batch = await quests.GetExpirableAsync(now, command.BatchSize, cancellationToken);
            var expired = batch.Count(q => q.TryExpire(now));
            if (expired == 0)
                break;

            await unitOfWork.SaveChangesAsync(cancellationToken);
            total += expired;

            if (batch.Count < command.BatchSize)
                break;
        }

        return Result<int>.Ok(total);
    }
}
