using LifeQuest.Domain.Admin;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Identity;
using LifeQuest.Domain.Recommendations;

namespace LifeQuest.Domain.Tests;

public sealed class AccountSuspensionTests
{
    private static readonly DateTime Now = new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);

    private static UserAccount Account()
        => UserAccount.Register("kullanici@example.com", "hash", "Kullanıcı", 1990, Now).Data!;

    [Fact]
    public void Timed_suspension_ends_by_itself()
    {
        var account = Account();
        account.Suspend(Now.AddDays(7), "Spam", Now);

        Assert.True(account.IsSuspended(Now.AddDays(6)));
        Assert.False(account.IsSuspended(Now.AddDays(7).AddSeconds(1)));
    }

    [Fact]
    public void Indefinite_suspension_lasts_until_lifted()
    {
        var account = Account();
        account.Suspend(null, "Kötüye kullanım", Now);

        Assert.True(account.IsSuspended(Now.AddYears(5)));
        Assert.True(account.Unsuspend());
        Assert.False(account.IsSuspended(Now));
        Assert.Null(account.SuspensionReason);
        Assert.False(account.Unsuspend());
    }

    [Fact]
    public void Suspension_is_independent_of_the_failed_login_lockout()
    {
        var account = Account();
        account.Suspend(null, "İnceleme", Now);
        account.RegisterSuccessfulLogin(Now);

        Assert.True(account.IsSuspended(Now));
    }
}

public sealed class TemplateAdministrationTests
{
    private static QuestTemplateSpec Spec(string title = "Semt kütüphanesini keşfet")
        => new("library-visit", title, "Semtindeki kütüphaneye git ve rafların arasında yarım saat dolaş.",
            QuestType.Weekly, Difficulty.Easy, LifeCategory.Learning, null, 45, 90, CostBand.Free, DayPart.Any,
            false, false, 14, 0, [Guid.NewGuid()], PhysicalEffort.Light, false);

    [Fact]
    public void Admin_edit_takes_the_template_out_of_seed_sync()
    {
        var spec = Spec();
        var template = QuestTemplate.Create(spec);
        Assert.Equal(EditorialSource.Seed, template.Source);

        Assert.True(template.ApplyAdminEdit(spec with { Title = "Semt kütüphanesinde yarım saat" }));
        Assert.Equal(EditorialSource.Admin, template.Source);
        Assert.Equal(2, template.Version);
    }

    [Fact]
    public void Safety_decision_by_admin_also_takes_the_template_out_of_seed_sync()
    {
        var template = QuestTemplate.Create(Spec(), SafetyLevel.NeedsReview);
        template.DecideSafety(SafetyLevel.Safe);

        Assert.True(template.IsOfferable);
        Assert.Equal(EditorialSource.Admin, template.Source);
    }

    [Fact]
    public void Deactivate_and_activate_bump_the_version_only_when_state_changes()
    {
        var template = QuestTemplate.Create(Spec());

        Assert.False(template.Activate());
        Assert.True(template.Deactivate());
        Assert.False(template.IsOfferable);
        Assert.False(template.Deactivate());
        Assert.True(template.Activate());
        Assert.True(template.IsOfferable);
        Assert.Equal(3, template.Version);
    }
}

public sealed class RecommendationWeightCatalogTests
{
    private static readonly RecommendationWeights Defaults = new();
    private static readonly DateTime Now = new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Every_editable_field_round_trips_through_get_and_set()
    {
        foreach (var field in RecommendationWeightCatalog.Fields)
        {
            var value = field.IsInteger ? field.Min + 1 : Math.Round((field.Min + field.Max) / 3, 3);
            var updated = field.Set(Defaults, value);
            Assert.Equal(value, field.Get(updated), 6);
            Assert.InRange(field.Get(Defaults), field.Min, field.Max);
        }
    }

    [Fact]
    public void Unknown_keys_and_out_of_range_values_are_rejected()
    {
        var errors = RecommendationWeightCatalog.Validate(new Dictionary<string, double>
        {
            ["Interest"] = 0.5,
            [nameof(RecommendationWeights.Risk)] = 1.5,
            [nameof(RecommendationWeights.RecentWindowDays)] = 7.5,
            [nameof(RecommendationWeights.Context)] = 0.2
        });

        Assert.Equal(["Interest", nameof(RecommendationWeights.RecentWindowDays), nameof(RecommendationWeights.Risk)],
            errors.Keys.Order());
    }

    [Fact]
    public void Apply_ignores_stored_values_that_are_no_longer_valid()
    {
        var weights = RecommendationWeightCatalog.Apply(Defaults, new Dictionary<string, double>
        {
            [nameof(RecommendationWeights.NoveltySurpriseMe)] = 0.3,
            [nameof(RecommendationWeights.Risk)] = 9,
            ["Removed"] = 1
        });

        Assert.Equal(0.3, weights.NoveltySurpriseMe);
        Assert.Equal(Defaults.Risk, weights.Risk);
    }

    [Fact]
    public void Settings_store_only_differences_from_defaults_and_track_changes()
    {
        var settings = RecommendationSettings.CreateEmpty();

        var changes = settings.Update(new Dictionary<string, double>
        {
            [nameof(RecommendationWeights.Diversity)] = 0.2,
            [nameof(RecommendationWeights.Risk)] = Defaults.Risk
        }, Defaults, "admin@example.com", Now);

        Assert.Equal([new WeightChange(nameof(RecommendationWeights.Diversity), Defaults.Diversity, 0.2)], changes);
        Assert.Equal([nameof(RecommendationWeights.Diversity)], settings.Overrides.Keys);
        Assert.Equal(1, settings.Revision);

        // Varsayılana eşit değer gönderilirse override silinir.
        settings.Update(new Dictionary<string, double> { [nameof(RecommendationWeights.Diversity)] = Defaults.Diversity },
            Defaults, "admin@example.com", Now);
        Assert.Empty(settings.Overrides);
        Assert.Equal(2, settings.Revision);
    }

    [Fact]
    public void Reset_restores_selected_keys_only()
    {
        var settings = RecommendationSettings.CreateEmpty();
        settings.Update(new Dictionary<string, double>
        {
            [nameof(RecommendationWeights.Diversity)] = 0.2,
            [nameof(RecommendationWeights.Friction)] = 0.3
        }, Defaults, "admin@example.com", Now);

        var changes = settings.Reset([nameof(RecommendationWeights.Friction)], Defaults, "admin@example.com", Now);

        Assert.Single(changes);
        Assert.Equal([nameof(RecommendationWeights.Diversity)], settings.Overrides.Keys);
        Assert.Empty(settings.Reset(["Friction"], Defaults, "admin@example.com", Now));
    }

    [Fact]
    public void Email_mask_keeps_only_the_first_letter_and_domain()
        => Assert.Equal("a***@example.com", EmailMask.Mask("ayse@example.com"));
}
