using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Profiles;
using LifeQuest.Domain.Progression;
using LifeQuest.Domain.Quests;
using LifeQuest.Domain.Recommendations;
using static LifeQuest.Domain.Tests.TestData;

namespace LifeQuest.Domain.Tests;

public sealed class UserQuestTests
{
    internal static UserQuest NewQuest(
        LifeCategory category = LifeCategory.Culture, LifeCategory? secondary = LifeCategory.Explorer,
        QuestType type = QuestType.Weekly, DateTime? expiresAt = null)
    {
        var candidate = Candidate("culture-new-venue", category, [Guid.NewGuid()], type, secondary: secondary);
        var recommendation = new RecommendedQuest(candidate, ScoreBreakdown.Empty, false, [], "test");
        var reward = RewardCalculator.Calculate(type, Difficulty.Medium, secondary is not null, 1m);

        return UserQuest.Offer(Guid.NewGuid(), recommendation, reward, QuestSource.Daily,
            DateOnly.FromDateTime(UtcNow), 0, UtcNow, expiresAt ?? UtcNow.AddHours(8));
    }

    [Fact]
    public void Accept_then_complete_then_complete_again_is_idempotent()
    {
        var quest = NewQuest();

        Assert.True(quest.Accept(UtcNow).Succeeded);
        Assert.Equal(UtcNow.AddDays(7), quest.ExpiresAt);

        var first = quest.Complete(UtcNow.AddHours(1));
        var second = quest.Complete(UtcNow.AddHours(2));

        Assert.True(first.Succeeded && first.Data);
        Assert.True(second.Succeeded);
        Assert.False(second.Data);
        Assert.Equal(UtcNow.AddHours(1), quest.CompletedAt);
        Assert.Single(quest.DomainEvents.OfType<QuestCompletedEvent>());
    }

    [Fact]
    public void Cannot_complete_without_accepting()
    {
        var result = NewQuest().Complete(UtcNow);
        Assert.Equal(QuestErrors.MustAcceptFirst, result.FirstError);
    }

    [Fact]
    public void Cannot_accept_after_offer_expired()
    {
        var quest = NewQuest(expiresAt: UtcNow.AddHours(1));
        Assert.Equal(QuestErrors.Expired, quest.Accept(UtcNow.AddHours(2)).FirstError);
    }

    [Fact]
    public void Skipped_quest_cannot_be_accepted()
    {
        var quest = NewQuest();
        quest.Skip(SkipReason.TooExpensive, UtcNow);

        Assert.Equal(QuestStatus.Skipped, quest.Status);
        Assert.Equal("INVALID_QUEST_TRANSITION", quest.Accept(UtcNow).FirstError!.Code);
    }

    [Fact]
    public void Expire_only_closes_open_quests_past_deadline()
    {
        var quest = NewQuest(expiresAt: UtcNow.AddHours(1));

        Assert.False(quest.TryExpire(UtcNow));
        Assert.True(quest.TryExpire(UtcNow.AddHours(1)));
        Assert.False(quest.TryExpire(UtcNow.AddHours(2)));
        Assert.Equal(QuestStatus.Expired, quest.Status);
    }

    [Fact]
    public void Rating_requires_completion_and_valid_range()
    {
        var quest = NewQuest();
        Assert.Equal(QuestErrors.RatingRequiresCompletion, quest.RecordFeedback(5, null, UtcNow).FirstError);

        quest.Accept(UtcNow);
        quest.Complete(UtcNow);

        Assert.Equal(QuestErrors.InvalidRating, quest.RecordFeedback(6, null, UtcNow).FirstError);
        Assert.True(quest.RecordFeedback(4, FeedbackPreference.MoreLikeThis, UtcNow).Data);
        Assert.False(quest.RecordFeedback(5, null, UtcNow).Data);
    }

    [Fact]
    public void Preference_can_be_given_without_completion()
    {
        var quest = NewQuest();
        Assert.True(quest.RecordFeedback(null, FeedbackPreference.LessLikeThis, UtcNow).Succeeded);
        Assert.Equal(FeedbackPreference.LessLikeThis, quest.Preference);
    }
}

public sealed class PlayerProgressTests
{
    [Fact]
    public void Reward_updates_life_and_category_xp_and_writes_ledger_entry()
    {
        var progress = PlayerProgress.CreateFor(Guid.NewGuid());
        var quest = UserQuestTests.NewQuest();
        quest.Accept(UtcNow);
        quest.Complete(UtcNow);

        var outcome = progress.ApplyQuestReward(quest, UtcNow);

        Assert.Equal(180, progress.LifeXp);
        Assert.Equal(2, progress.LifeLevel);
        Assert.True(outcome.LeveledUp);
        Assert.Equal(120, progress.CategoryOf(LifeCategory.Culture).Xp);
        Assert.Equal(1, progress.CategoryOf(LifeCategory.Culture).CompletedCount);
        Assert.Equal(40, progress.CategoryOf(LifeCategory.Explorer).Xp);
        Assert.Equal(0, progress.CategoryOf(LifeCategory.Explorer).CompletedCount);
        Assert.Equal(quest.Id, outcome.Transaction.SourceId);
        Assert.Contains(outcome.NewAchievements, a => a.Code == "FIRST_STEP");
    }

    [Fact]
    public void Achievements_unlock_once()
    {
        var progress = PlayerProgress.CreateFor(Guid.NewGuid());
        var codes = new List<string>();

        foreach (var category in LifeCategories.All)
        {
            var quest = UserQuestTests.NewQuest(category, secondary: null);
            quest.Accept(UtcNow);
            quest.Complete(UtcNow);
            codes.AddRange(progress.ApplyQuestReward(quest, UtcNow).NewAchievements.Select(a => a.Code));
        }

        Assert.Single(codes, "FIRST_STEP");
        Assert.Single(codes, "ALL_ROUNDER");
        Assert.Equal(codes.Count, progress.Achievements.Count);
    }

    [Fact]
    public void Five_feedbacks_unlock_reflective()
    {
        var progress = PlayerProgress.CreateFor(Guid.NewGuid());
        for (var i = 0; i < 4; i++) Assert.Empty(progress.RegisterFeedback(UtcNow));

        Assert.Equal("REFLECTIVE", Assert.Single(progress.RegisterFeedback(UtcNow)).Code);
    }
}

public sealed class UserAccountTests
{
    [Fact]
    public void Underage_users_cannot_register()
    {
        var result = UserAccount.Register("a@b.com", "hash", "Ali", UtcNow.Year - 17, UtcNow);
        Assert.Equal(IdentityErrors.Underage, result.FirstError);
    }

    [Fact]
    public void Email_is_normalized()
    {
        var account = UserAccount.Register("  Ali@Example.COM ", "hash", "Ali", 1990, UtcNow).Data!;
        Assert.Equal("ali@example.com", account.Email);
    }

    [Fact]
    public void Account_locks_after_repeated_failures()
    {
        var account = UserAccount.Register("a@b.com", "hash", "Ali", 1990, UtcNow).Data!;

        for (var i = 0; i < UserAccount.MaxFailedLoginAttempts - 1; i++)
            account.RegisterFailedLogin(UtcNow);
        Assert.False(account.IsLockedOut(UtcNow));

        account.RegisterFailedLogin(UtcNow);
        Assert.True(account.IsLockedOut(UtcNow));
        Assert.False(account.IsLockedOut(UtcNow.Add(UserAccount.LockoutDuration)));
    }

    [Fact]
    public void Refresh_token_revocation_is_idempotent()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", Guid.NewGuid(), UtcNow, TimeSpan.FromDays(30));
        Assert.True(token.IsActive(UtcNow));

        token.Revoke(UtcNow, "rotated");
        token.Revoke(UtcNow.AddMinutes(1), "again");

        Assert.False(token.IsActive(UtcNow));
        Assert.Equal("rotated", token.RevokeReason);
    }
}

public sealed class UserProfileTests
{
    private readonly Guid _art = Guid.NewGuid();
    private readonly Guid _music = Guid.NewGuid();
    private readonly Guid _photo = Guid.NewGuid();

    [Fact]
    public void Onboarding_requires_at_least_one_interest()
    {
        var profile = UserProfile.CreateFor(Guid.NewGuid());
        var result = profile.CompleteOnboarding(new ProfilePreferences(), [], UtcNow);

        Assert.Equal(ProfileErrors.InterestsRequired, result.FirstError);
        Assert.False(profile.OnboardingCompleted);
    }

    [Fact]
    public void Positive_feedback_learns_new_interest_negative_only_lowers_existing()
    {
        var profile = UserProfile.CreateFor(Guid.NewGuid());
        profile.SetExplicitInterests([new InterestSelection(_art, 0.5)], UtcNow);

        profile.AdjustInterests([_art, _photo], InterestLearning.MoreLikeThisDelta, UtcNow);
        profile.AdjustInterests([_music], InterestLearning.LessLikeThisDelta, UtcNow);

        var weights = profile.InterestWeights();
        Assert.Equal(0.58, weights[_art], 4);
        Assert.Equal(0.38, weights[_photo], 4);
        Assert.False(weights.ContainsKey(_music));
        Assert.Equal(InterestSource.Learned, profile.Interests.Single(i => i.InterestId == _photo).Source);
    }

    [Fact]
    public void Resetting_explicit_interests_keeps_learned_ones()
    {
        var profile = UserProfile.CreateFor(Guid.NewGuid());
        profile.SetExplicitInterests([new InterestSelection(_art, 0.5)], UtcNow);
        profile.AdjustInterests([_photo], 0.1, UtcNow);

        profile.SetExplicitInterests([new InterestSelection(_music, 0.7)], UtcNow);

        Assert.Equal([_music, _photo], profile.Interests.Select(i => i.InterestId).OrderBy(g => g == _photo));
    }

    [Fact]
    public void Invalid_time_zone_is_rejected()
    {
        var profile = UserProfile.CreateFor(Guid.NewGuid());
        var result = profile.UpdatePreferences(new ProfilePreferences(TimeZoneId: "Mars/Olympus"));
        Assert.Equal(ProfileErrors.InvalidTimeZone, result.FirstError);
    }
}
