using LifeQuest.Domain.Catalog;
using LifeQuest.Domain.Common;
using LifeQuest.Infrastructure.Persistence.Seed;
using Xunit.Abstractions;

namespace LifeQuest.Catalog.Tests;

/// <summary>
/// Editoryal katalog için CI kapısı: seed'deki her template güvenlik kontrol listesini, katalog da denge
/// kurallarını geçmelidir. Kuralı çiğneyen bir template çalışma zamanında zaten NeedsReview olur; bu test
/// sorunu yayından önce yakalar.
/// </summary>
public sealed class CatalogSeedTests(ITestOutputHelper output)
{
    private static readonly Dictionary<string, Guid> InterestIds =
        CatalogSeedData.Interests.ToDictionary(i => i.Code, _ => Guid.NewGuid());

    private static readonly List<QuestTemplateSpec> Specs =
        CatalogSeedData.Templates.Select(t => CatalogSeeder.ToSpec(t, InterestIds)).ToList();

    public static TheoryData<string> TemplateCodes()
    {
        var data = new TheoryData<string>();
        foreach (var template in CatalogSeedData.Templates) data.Add(template.Code);
        return data;
    }

    [Theory]
    [MemberData(nameof(TemplateCodes))]
    public void Every_template_passes_the_editorial_safety_checklist(string code)
    {
        var violations = CatalogSafetyRules.ValidateTemplate(Specs.Single(s => s.Code == code));
        Assert.True(violations.Count == 0, $"{code}: {string.Join(" | ", violations)}");
    }

    [Fact]
    public void Catalog_is_balanced_accessible_and_has_one_hundred_forty_two_templates()
    {
        var violations = CatalogSafetyRules.ValidateCatalog(Specs);
        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
        Assert.Equal(142, Specs.Count);

        output.WriteLine($"Ücretsiz: %{Specs.Count(s => s.Cost == CostBand.Free) * 100 / Specs.Count}");
        output.WriteLine($"Şehirden bağımsız: %{Specs.Count(s => !s.RequiresCity) * 100 / Specs.Count}");
        foreach (var group in Specs.GroupBy(s => s.Category))
            output.WriteLine($"{group.Key}: {group.Count()} ({group.Count(s => s.Type == QuestType.Daily)} günlük)");
    }

    [Fact]
    public void Every_interest_and_relation_reference_exists_and_every_interest_is_used()
    {
        var codes = CatalogSeedData.Interests.Select(i => i.Code).ToHashSet();

        Assert.All(CatalogSeedData.Templates.SelectMany(t => t.Interests), code => Assert.Contains(code, codes));
        Assert.All(CatalogSeedData.Relations, r =>
        {
            Assert.Contains(r.From, codes);
            Assert.Contains(r.To, codes);
        });

        var used = CatalogSeedData.Templates.SelectMany(t => t.Interests).ToHashSet();
        Assert.Empty(codes.Except(used));
    }

    [Fact]
    public void Every_interest_has_enough_quests_including_a_short_one()
    {
        var names = CatalogSeedData.Interests.ToDictionary(i => InterestIds[i.Code], i => i.Name);
        var violations = CatalogSafetyRules.ValidateInterestCoverage(Specs, names);
        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));

        foreach (var interest in CatalogSeedData.Interests.OrderBy(i => i.Code))
        {
            var tagged = Specs.Where(s => s.InterestIds.Contains(InterestIds[interest.Code])).ToList();
            output.WriteLine($"{interest.Code}: {tagged.Count} görev, {tagged.Count(t => t.MaxMinutes <= 60)} kısa, {tagged.Count(t => t.Cost == CostBand.Free)} ücretsiz");
        }
    }

    [Fact]
    public void Starter_cards_cover_every_category_with_accessible_quests()
    {
        var starters = CatalogSeedData.Templates.Where(t => t.Starter).ToList();

        Assert.Equal(12, starters.Count);
        Assert.All(LifeCategories.All, category => Assert.Equal(2, starters.Count(s => s.Category == category)));
        Assert.All(starters, s => Assert.True(s.Effort <= PhysicalEffort.Light, $"{s.Code} başlangıç kartı için fazla eforlu"));
        Assert.True(starters.Count(s => !s.RequiresCity) >= 10, "Başlangıç kartlarının çoğu şehirden bağımsız olmalı");
    }

    [Fact]
    public void Every_user_can_get_offers_even_with_the_most_restrictive_preferences()
    {
        // Ücretsiz bütçe + şehir yok + yalnızca hafif efor: her kategoride en az bir seçenek kalmalı.
        var accessible = Specs.Where(s => s.Cost == CostBand.Free && !s.RequiresCity && s.Effort <= PhysicalEffort.Light).ToList();
        Assert.All(LifeCategories.All, c => Assert.Contains(accessible, s => s.Category == c));
    }
}
