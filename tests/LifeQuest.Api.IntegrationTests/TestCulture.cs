using System.Globalization;
using System.Runtime.CompilerServices;

namespace LifeQuest.Tests;

/// <summary>
/// Testler makinenin kültüründen bağımsız: varsayılan dil Türkçe (uygulamanın varsayılanı). İngilizce beklentiler
/// <c>Language.Use("en")</c> ile açıkça sınanır.
/// </summary>
internal static class TestCulture
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        var turkish = CultureInfo.GetCultureInfo("tr-TR");
        CultureInfo.DefaultThreadCurrentCulture = turkish;
        CultureInfo.DefaultThreadCurrentUICulture = turkish;
        CultureInfo.CurrentCulture = turkish;
        CultureInfo.CurrentUICulture = turkish;
    }
}
