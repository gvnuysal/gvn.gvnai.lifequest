using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace LifeQuest.Mobile;

/// <summary>Tek aktivite; lifequest:// bağlantıları (parti daveti) buraya gelir.</summary>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout
                           | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    ScreenOrientation = ScreenOrientation.Portrait)]
[IntentFilter([Intent.ActionView], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataScheme = "lifequest")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        OpenLink(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        OpenLink(intent);
    }

    private static void OpenLink(Intent? intent)
    {
        if (intent?.Data?.ToString() is { } link && Uri.TryCreate(link, UriKind.Absolute, out var uri))
            App.OpenLink(uri);
    }
}
