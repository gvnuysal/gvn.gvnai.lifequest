using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LifeQuest.Mobile.Core.Api;
using LifeQuest.Mobile.Core.Localization;
using LifeQuest.Mobile.Core.Services;

namespace LifeQuest.Mobile.Core.ViewModels;

/// <summary>
/// Ekran ViewModel'lerinin tabanı: meşgul durumu, yükleme hatası ve dil değişince yeniden hesaplanan metinler.
/// Dil değişince <see cref="OnLanguageChanged"/> çağrılır; türetilen sınıf listelerini yeni dilde yeniden kurar.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    protected ViewModelBase() => WeakLanguage.Subscribe(this);

    /// <summary>Kod içinde metin okuma kısayolu (XAML <c>{l:Tr ...}</c> kullanır).</summary>
    protected static Strings S => Localizer.Instance.S;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>Sayfanın ilk yüklemesi başarısız olduysa gösterilecek mesaj.</summary>
    [ObservableProperty]
    public partial string? LoadError { get; set; }

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    protected internal virtual void OnLanguageChanged()
    {
    }

    /// <summary>İşlemi çalıştırır; API hatasını kullanıcıya gösterir. Başarılıysa true.</summary>
    protected async Task<bool> RunAsync(Func<Task> action, IToast? toast = null, bool busy = true)
    {
        if (busy && IsBusy) return false;
        if (busy) IsBusy = true;
        try
        {
            await action();
            return true;
        }
        catch (ApiException ex)
        {
            if (toast is not null) toast.Error(ex.Message);
            else LoadError = ex.Message;
            return false;
        }
        finally
        {
            if (busy) IsBusy = false;
        }
    }

    /// <summary>Sayfa verisini yükler; hata sayfada boş durum olarak gösterilir.</summary>
    protected async Task LoadAsync(Func<Task> load)
    {
        LoadError = null;
        if (await RunAsync(load))
            IsLoaded = true;
    }

    /// <summary>ViewModel'leri dil olayına zayıf referansla bağlar (sayfalar kapanınca bellekte kalmasınlar).</summary>
    private static class WeakLanguage
    {
        private static readonly List<WeakReference<ViewModelBase>> Subscribers = [];

        static WeakLanguage() => Lang.Changed += (_, _) =>
        {
            lock (Subscribers)
            {
                Subscribers.RemoveAll(w => !w.TryGetTarget(out _));
                foreach (var weak in Subscribers.ToList())
                    if (weak.TryGetTarget(out var vm)) vm.OnLanguageChanged();
            }
        };

        public static void Subscribe(ViewModelBase vm)
        {
            lock (Subscribers) Subscribers.Add(new WeakReference<ViewModelBase>(vm));
        }
    }
}
