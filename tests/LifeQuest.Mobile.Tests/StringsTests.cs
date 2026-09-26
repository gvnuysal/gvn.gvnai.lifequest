using System.Reflection;
using LifeQuest.Mobile.Core.Localization;

namespace LifeQuest.Mobile.Tests;

/// <summary>Üretilen sözlükteki her metin iki dilde de dolu; parametreli metinler çağrılabilir.</summary>
public sealed class StringsTests
{
    [Theory]
    [InlineData(AppLanguage.Tr)]
    [InlineData(AppLanguage.En)]
    public void Every_text_has_a_value(AppLanguage language)
    {
        using var _ = new LanguageScope(language);
        var empty = new List<string>();
        var count = Walk(new Strings(), "S", empty);
        Assert.True(count > 300, $"yalnızca {count} metin bulundu");
        Assert.Equal(IntentionallyEmpty, empty);
    }

    /// <summary>Web sözlüğünde bilerek boş bırakılmış ipuçları (sebep kendini açıklıyor).</summary>
    private static readonly string[] IntentionallyEmpty = ["S.Labels.SkipReasons.TooFar.Hint", "S.Labels.SkipReasons.Other.Hint"];

    [Fact]
    public void Language_switch_replaces_the_binding_source()
    {
        using var _ = new LanguageScope(AppLanguage.Tr);
        var before = Localizer.Instance.S;
        var raised = false;
        Localizer.Instance.PropertyChanged += (_, e) => raised |= e.PropertyName == nameof(Localizer.S);

        Lang.Set(AppLanguage.En);

        Assert.True(raised);
        Assert.NotSame(before, Localizer.Instance.S);
        Assert.Equal("Today", Localizer.Instance.S.Nav.Today);
    }

    private static int Walk(object node, string path, List<string> empty)
    {
        var count = 0;
        foreach (var property in node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0) continue;
            var value = property.GetValue(node);
            switch (value)
            {
                case string s:
                    count++;
                    if (string.IsNullOrWhiteSpace(s)) empty.Add($"{path}.{property.Name}");
                    break;
                case IReadOnlyList<string> list:
                    count += list.Count;
                    if (list.Count == 0 || list.Any(string.IsNullOrWhiteSpace)) empty.Add($"{path}.{property.Name}");
                    break;
                case LocalizedStrings child:
                    count += Walk(child, $"{path}.{property.Name}", empty);
                    break;
            }
        }

        foreach (var method in node.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (method.IsSpecialName || method.ReturnType != typeof(string)) continue;
            var args = method.GetParameters().Select(p => p.ParameterType == typeof(int) ? (object)2 : "x").ToArray();
            count++;
            if (string.IsNullOrWhiteSpace((string?)method.Invoke(node, args))) empty.Add($"{path}.{method.Name}()");
        }

        return count;
    }
}
