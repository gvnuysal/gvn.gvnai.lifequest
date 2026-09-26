using Gvn.GvnFramework.BackgroundJobs.Abstractions;
using LifeQuest.Application.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LifeQuest.Infrastructure.Jobs;

/// <summary>
/// 15 dakikada bir, yerel saati hatırlatma saatine gelen kullanıcılara push gönderir. Bir saat içinde dört çalışma
/// olduğundan tek bir kaçırılan çalışma hatırlatmayı düşürmez; günde bir gönderim kullanıcı profilinde tutulur.
/// </summary>
public sealed class DailyReminderJob(IServiceScopeFactory scopeFactory, ILogger<DailyReminderJob> logger) : IRecurringJob
{
    public const string JobId = "notifications:daily-reminder";
    public const string Cron = "*/15 * * * *";

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<DailyReminderService>().SendDueAsync(cancellationToken);
        if (result.Due > 0)
            logger.LogInformation("Daily reminders: {Sent} sent, {Skipped} skipped (already done today) of {Due} due",
                result.Sent, result.SkippedAlreadyDone, result.Due);
    }
}
