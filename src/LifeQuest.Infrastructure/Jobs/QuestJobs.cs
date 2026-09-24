using Gvn.GvnFramework.BackgroundJobs.Abstractions;
using Gvn.GvnFramework.Core.Extensions;
using LifeQuest.Application.Quests;
using LifeQuest.Domain.Quests;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LifeQuest.Infrastructure.Jobs;

/// <summary>Süresi geçen açık quest'leri kapatır. Saatlik; tekrar çalışması güvenlidir.</summary>
public sealed class ExpireStaleQuestsJob(ISender sender, ILogger<ExpireStaleQuestsJob> logger) : IRecurringJob
{
    public const string JobId = "quests:expire-stale";
    public const string Cron = "5 * * * *";

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ExpireStaleQuestsCommand(), cancellationToken);
        logger.LogInformation("Expired {Count} stale quests", result.Data);
    }
}

/// <summary>
/// Son 14 günde aktif olan kullanıcılar için günün önerilerini önceden üretir; kullanıcı uygulamayı
/// açtığında öneri hazırdır. İdempotent: günlük benzersiz indeks sayesinde çift üretim olmaz.
/// Bildirim göndermez; hatırlatma sıklığı kullanıcı tercihine bağlı olmalıdır.
/// </summary>
public sealed class DailyQuestGenerationJob(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<DailyQuestGenerationJob> logger) : IRecurringJob
{
    public const string JobId = "quests:daily-generation";
    public const string Cron = "0 4 * * *";

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> userIds;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            userIds = await scope.ServiceProvider.GetRequiredService<IUserQuestRepository>()
                .GetRecentlyActiveUserIdsAsync(clock.GetUtcNow().UtcDateTime.AddDays(-14), cancellationToken);
        }

        var generated = 0;
        foreach (var batch in userIds.Batch(50))
        {
            foreach (var userId in batch)
            {
                // Her kullanıcı ayrı scope: bir kullanıcının hatası diğerlerini etkilemez, change tracker şişmez.
                await using var scope = scopeFactory.CreateAsyncScope();
                try
                {
                    var result = await scope.ServiceProvider.GetRequiredService<QuestOfferService>()
                        .GetOrCreateDailyOffersAsync(userId, cancellationToken);
                    if (result.Succeeded) generated++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Daily quest generation failed for {UserId}", userId);
                }
            }
        }

        logger.LogInformation("Daily quest generation finished for {Generated}/{Total} users", generated, userIds.Count);
    }
}
