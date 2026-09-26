using Gvn.GvnFramework.Domain.Repositories;
using LifeQuest.Application.Abstractions;
using LifeQuest.Application.Notifications;
using LifeQuest.Domain.Progression;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Social;
using Microsoft.Extensions.Logging;

namespace LifeQuest.Application.Social;

/// <summary>
/// Görev tamamlandığında/atlandığında/süresi dolduğunda üyenin partisini günceller; parti tamamlanırsa tamamlayan
/// herkese "birlikte" bonusu yazılır. Çağıranın unit of work'ü içinde çalışır (aynı SaveChanges): bonus, tamamlama ile
/// birlikte ya kaydedilir ya hiç kaydedilmez.
/// </summary>
public sealed class PartyService(
    IQuestPartyRepository parties,
    IPlayerProgressRepository progress,
    PushNotifier notifier,
    Domain.Identity.IUserAccountRepository accounts,
    IUnitOfWork unitOfWork,
    ILogger<PartyService> logger)
{
    public async Task<PartySettlement?> OnQuestResolvedAsync(UserQuest quest, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var party = await parties.GetByUserQuestAsync(quest.Id, cancellationToken);
        return party is null ? null : await ApplyAsync(party, quest, nowUtc, cancellationToken);
    }

    /// <summary>Süresi dolan görevler için toplu: yalnızca partideki görevler etkilenir.</summary>
    public async Task<IReadOnlyList<PartySettlement>> OnQuestsExpiredAsync(
        IReadOnlyCollection<UserQuest> expired, DateTime nowUtc, CancellationToken cancellationToken)
    {
        if (expired.Count == 0) return [];

        var byId = expired.ToDictionary(q => q.Id);
        var settlements = new List<PartySettlement>();
        foreach (var party in await parties.GetByUserQuestsAsync(byId.Keys, cancellationToken))
            foreach (var member in party.Members.Where(m => byId.ContainsKey(m.UserQuestId)).ToList())
                if (await ApplyAsync(party, byId[member.UserQuestId], nowUtc, cancellationToken) is { } settlement)
                    settlements.Add(settlement);
        return settlements;
    }

    /// <summary>Kayıttan sonra: tamamlayanlara push. Hata olursa yalnızca loglanır.</summary>
    public async Task NotifyAsync(PartySettlement? settlement, CancellationToken cancellationToken)
    {
        if (settlement is null || !notifier.IsConfigured) return;
        try
        {
            foreach (var member in settlement.Completers)
            {
                // Her üyeye kendi dilinde.
                var title = settlement.Party.LocalizedQuestTitle;
                var english = await accounts.GetLanguageAsync(member.UserId, cancellationToken) == Domain.Localization.Language.English;
                await notifier.SendToUserAsync(member.UserId, english
                    ? new PushNotification("You did it together!",
                        $"{title.En}: party complete, +{member.BonusXp} XP \"together\" bonus.", "/progress", "party")
                    : new PushNotification("Birlikte başardınız!",
                        $"{title.Tr}: parti tamamlandı, +{member.BonusXp} XP \"birlikte\" bonusu.", "/progress", "party"),
                    cancellationToken);
            }
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Party notification failed for {PartyId}", settlement.Party.Id);
        }
    }

    private async Task<PartySettlement?> ApplyAsync(QuestParty party, UserQuest quest, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var settlement = quest.Status switch
        {
            QuestStatus.Completed => party.MarkCompleted(quest.Id, quest.Reward.LifeXp, nowUtc),
            QuestStatus.Skipped or QuestStatus.Expired => party.MarkDropped(quest.Id, nowUtc),
            _ => null
        };
        if (settlement is not null)
            await AwardAsync(settlement, nowUtc, cancellationToken);
        return settlement;
    }

    public async Task AwardAsync(PartySettlement settlement, DateTime nowUtc, CancellationToken cancellationToken)
    {
        foreach (var member in settlement.Completers)
        {
            var memberProgress = await progress.GetByUserIdAsync(member.UserId, cancellationToken);
            if (memberProgress is null) continue;

            var transaction = memberProgress.ApplyPartyBonus(
                member.UserQuestId, settlement.Party.LocalizedQuestTitle, settlement.Party.Category, member.BonusXp, nowUtc);
            await progress.AddTransactionAsync(transaction, cancellationToken);
        }
    }
}
