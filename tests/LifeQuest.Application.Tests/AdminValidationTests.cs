using LifeQuest.Application.Admin.Catalog;
using LifeQuest.Application.Admin.Users;
using LifeQuest.Application.Admin.Weights;
using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Domain.Recommendations;

namespace LifeQuest.Application.Tests;

public sealed class TemplateInputTests
{
    private static TemplateInput Input(
        string code = "library-visit", int min = 45, int max = 90, IReadOnlyList<DayPart>? dayParts = null,
        LifeCategory? secondary = null, double risk = 0.123)
        => new(code, " Semt kütüphanesini keşfet ", "Semtindeki kütüphaneye git ve rafların arasında yarım saat dolaş.",
            QuestType.Weekly, Difficulty.Easy, LifeCategory.Learning, secondary, min, max, CostBand.Free,
            dayParts ?? [DayPart.Morning, DayPart.Afternoon], false, false, 14, risk,
            [Guid.Empty, Guid.Empty], PhysicalEffort.Light, false);

    [Fact]
    public void Day_parts_list_becomes_a_flag_and_text_is_trimmed()
    {
        var spec = Input().ToSpec();

        Assert.Equal(DayPart.Morning | DayPart.Afternoon, spec.DayParts);
        Assert.Equal("Semt kütüphanesini keşfet", spec.Title);
        Assert.Equal(0.12, spec.RiskScore);
        Assert.Single(spec.InterestIds);
    }

    [Theory]
    [InlineData("Kod Boşluklu")]
    [InlineData("ab")]
    [InlineData("büyük-harf-değil-ç")]
    public void Invalid_codes_are_rejected(string code)
        => Assert.Contains(new TemplateInputValidator().Validate(Input(code)).Errors, e => e.PropertyName == "Code");

    [Fact]
    public void Inputs_that_would_break_domain_guards_are_rejected_before_the_handler()
    {
        var result = new TemplateInputValidator().Validate(
            Input(min: 90, max: 45, dayParts: [], secondary: LifeCategory.Learning));

        Assert.Contains(result.Errors, e => e.PropertyName == "MaxMinutes");
        Assert.Contains(result.Errors, e => e.PropertyName == "DayParts");
        Assert.Contains(result.Errors, e => e.PropertyName == "SecondaryCategory");
    }

    [Fact]
    public void Editorial_rule_violations_are_not_validation_errors()
    {
        // Risk 0,5 kural ihlalidir ama kayda engel değildir: template NeedsReview olarak kaydedilir.
        Assert.True(new TemplateInputValidator().Validate(Input(risk: 0.5)).IsValid);
    }
}

public sealed class AdminCommandValidationTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData(1, true)]
    [InlineData(7, true)]
    [InlineData(30, true)]
    [InlineData(3, false)]
    public void Suspension_length_is_limited_to_fixed_options(int? days, bool valid)
        => Assert.Equal(valid, new SuspendUserCommandValidator().Validate(new SuspendUserCommand(Guid.NewGuid(), days, "Spam")).IsValid);

    [Fact]
    public void Suspension_and_deletion_require_a_reason()
    {
        Assert.False(new SuspendUserCommandValidator().Validate(new SuspendUserCommand(Guid.NewGuid(), 7, " ")).IsValid);
        Assert.False(new DeleteUserCommandValidator().Validate(new DeleteUserCommand(Guid.NewGuid(), "", "a@b.com")).IsValid);
    }

    [Fact]
    public void Only_known_roles_can_be_assigned()
    {
        Assert.True(new SetUserRoleCommandValidator().Validate(new SetUserRoleCommand(Guid.NewGuid(), "admin")).IsValid);
        Assert.False(new SetUserRoleCommandValidator().Validate(new SetUserRoleCommand(Guid.NewGuid(), "superuser")).IsValid);
    }

    [Fact]
    public void Weight_updates_are_validated_against_the_catalog_limits()
    {
        var result = new UpdateRecommendationWeightsCommandValidator().Validate(new UpdateRecommendationWeightsCommand(0,
            new Dictionary<string, double>
            {
                [nameof(RecommendationWeights.Risk)] = 2,
                [nameof(RecommendationWeights.GuidedExploration)] = 0
            }, "Deneme"));

        Assert.Contains(result.Errors, e => e.PropertyName == $"Values.{nameof(RecommendationWeights.Risk)}");
        Assert.Contains(result.Errors, e => e.PropertyName == $"Values.{nameof(RecommendationWeights.GuidedExploration)}");
    }

    [Fact]
    public void Weight_updates_need_a_reason_and_at_least_one_value()
    {
        var result = new UpdateRecommendationWeightsCommandValidator().Validate(
            new UpdateRecommendationWeightsCommand(0, new Dictionary<string, double>(), ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "Reason");
        Assert.Contains(result.Errors, e => e.PropertyName == "Values");
    }
}
