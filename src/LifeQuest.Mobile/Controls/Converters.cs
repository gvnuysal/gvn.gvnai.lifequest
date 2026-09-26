using System.Globalization;
using LifeQuest.Mobile.Core.Formatting;

namespace LifeQuest.Mobile.Controls;

/// <summary>Kategori/renk anahtarı ("Fitness", "Primary"…) → temaya göre renk (Colors.xaml: {Anahtar}Light/Dark).</summary>
public sealed class ThemeColorConverter : IValueConverter
{
    /// <summary>Parametre bir kalıptır; * yerine değer gelir: "Cat*" → CatFitness, "Cat*Ink" → CatFitnessInk.</summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Palette.Get((parameter as string ?? "*").Replace("*", System.Convert.ToString(value, CultureInfo.InvariantCulture)));

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public static class Palette
{
    /// <summary>Tam anahtarla renk (ör. "SurfaceLight").</summary>
    public static Color Raw(string key)
        => Application.Current?.Resources.TryGetValue(key, out var color) == true && color is Color c ? c : Colors.Gray;

    /// <summary>Temaya göre renk: "Surface" → SurfaceLight / SurfaceDark.</summary>
    public static Color Get(string key)
    {
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var resources = Application.Current?.Resources;
        return resources is not null && resources.TryGetValue(key + (dark ? "Dark" : "Light"), out var color) && color is Color c
            ? c
            : Colors.Gray;
    }
}

public sealed class InvertBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}

public sealed class NotNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s ? !string.IsNullOrWhiteSpace(s) : value is not null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>0–1 oran → piksel genişlik yerine yıldız oranı (Grid sütunu).</summary>
public sealed class RatioToStarConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ratio = Math.Clamp(value is double d ? d : 0, 0, 1);
        return parameter as string == "rest" ? new GridLength(1 - ratio, GridUnitType.Star) : new GridLength(ratio, GridUnitType.Star);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Değer parametreye eşit mi (ör. Step == "2").</summary>
public sealed class EqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(System.Convert.ToString(value, CultureInfo.InvariantCulture), parameter as string, StringComparison.Ordinal);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>"Çok seviyorum" seçili ilgi alanında kalp ikonu.</summary>
public sealed class LoveIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is true ? "heart" : null;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Skor çubuğu rengi: ceza kırmızı, katkı XP rengi.</summary>
public sealed class PenaltyColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Palette.Get("Danger") : Palette.Get("Xp");

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Kilitli başarım soluk görünür.</summary>
public sealed class LockedOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is true ? 1.0 : 0.5;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
