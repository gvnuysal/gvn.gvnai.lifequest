using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Progression;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;
using LifeQuest.Domain.Social;
using static LifeQuest.Domain.Tests.TestData;

namespace LifeQuest.Domain.Tests;

public sealed class QuestPartyTests
{
    private static readonly Guid Template = Guid.NewGuid();

    private static UserQuest Accepted(Guid? userId = null)
    {
        var candidate = Candidate("walk", LifeCategory.Fitness, [Guid.NewGuid()], QuestType.Weekly) with { TemplateId = Template };
        var reward = RewardCalculator.Calculate(QuestType.Weekly, Difficulty.Medium, false, 1m);
        var quest = UserQuest.Offer(userId ?? Guid.NewGuid(), new RecommendedQuest(candidate, ScoreBreakdown.Empty, false, [], "test"),
            reward, QuestSource.Daily, DateOnly.FromDateTime(UtcNow), 0, UtcNow, UtcNow.AddHours(8));
        quest.Accept(UtcNow);
        return quest;
    }

    private static (QuestParty Party, UserQuest Host) NewParty()
    {
        var host = Accepted();
        return (QuestParty.Create(host, "Ayşe", UtcNow).Data!, host);
    }

    [Fact]
    public void Invite_code_is_short_unambiguous_and_the_party_lives_as_long_as_the_hosts_quest()
    {
        var (party, host) = NewParty();
        Assert.Equal(QuestParty.InviteCodeLength, party.InviteCode.Length);
        Assert.DoesNotContain(party.InviteCode, c => "01OI".Contains(c));
        Assert.Equal(host.ExpiresAt, party.ExpiresAt);
        Assert.False(QuestParty.Create(UserQuestTests.NewQuest(), "Ayşe", UtcNow).Succeeded); // Kabul edilmemiş görev.
    }

    [Fact]
    public void Everyone_still_in_must_complete_and_a_lone_completer_keeps_the_party_open()
    {
        var (party, host) = NewParty();
        Assert.Null(party.MarkCompleted(host.Id, 100, UtcNow)); // Tek kişi: parti açık kalır, sonradan katılan olabilir.
        Assert.Equal(PartyStatus.Open, party.Status);

        var late = Accepted();
        Assert.True(party.Join(late.UserId, "Mert", late, UtcNow.AddHours(1)).Succeeded);
        var dropper = Accepted();
        party.Join(dropper.UserId, "Deniz", dropper, UtcNow.AddHours(1));

        Assert.Null(party.MarkDropped(dropper.Id, UtcNow.AddHours(2)));
        var settlement = party.MarkCompleted(late.Id, 100, UtcNow.AddHours(3));

        Assert.NotNull(settlement);
        Assert.Equal(PartyStatus.Completed, party.Status);
        Assert.Equal(2, settlement!.Completers.Count);
        Assert.All(settlement.Completers, m => Assert.Equal(QuestParty.BonusFor(100), m.BonusXp));
        Assert.Null(party.MarkCompleted(late.Id, 100, UtcNow.AddHours(4))); // İkinci kez bonus yok.
    }

    [Theory]
    [InlineData(30, 20)]
    [InlineData(200, 50)]
    public void Bonus_is_a_quarter_of_the_quest_xp_with_a_floor(int questXp, int bonus)
        => Assert.Equal(bonus, QuestParty.BonusFor(questXp));

    [Fact]
    public void Seats_expiry_and_leaving_are_enforced()
    {
        var (party, host) = NewParty();
        for (var i = 0; i < QuestParty.MaxMembers - 1; i++)
        {
            var member = Accepted();
            Assert.True(party.Join(member.UserId, $"Üye {i}", member, UtcNow).Succeeded);
        }
        var extra = Accepted();
        Assert.Equal("PARTY_FULL", party.CanJoin(extra.UserId, UtcNow).Errors[0].Code);

        var (fresh, _) = NewParty();
        Assert.Equal("PARTY_CLOSED", fresh.CanJoin(Guid.NewGuid(), fresh.ExpiresAt).Errors[0].Code);

        fresh.MarkCompleted(fresh.Members.Single().UserQuestId, 50, UtcNow);
        Assert.Equal("PARTY_ALREADY_COMPLETED", fresh.Leave(fresh.HostUserId, UtcNow).Errors[0].Code);
    }

    [Fact]
    public void Party_bonus_goes_to_the_ledger_once_per_member_quest()
    {
        var progress = PlayerProgress.CreateFor(Guid.NewGuid());
        var questId = Guid.NewGuid();
        var transaction = progress.ApplyPartyBonus(questId, "Yürüyüş", LifeCategory.Fitness, 25, UtcNow);

        Assert.Equal(25, progress.LifeXp);
        Assert.Equal(0, progress.TotalCompleted);
        Assert.Equal(XpSourceType.PartyBonus, transaction.SourceType);
        Assert.Equal(questId, transaction.SourceId);
    }
}
