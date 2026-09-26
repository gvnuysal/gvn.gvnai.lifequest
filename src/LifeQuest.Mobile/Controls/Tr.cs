using LifeQuest.Mobile.Core.Localization;

namespace LifeQuest.Mobile.Controls;

/// <summary>
/// Sözlükten metin: <c>Text="{l:Tr Today.Suggestions}"</c>. Kaynak <see cref="Localizer"/> olduğu için dil
/// değişince ekran yenilenmeden yeni dilde görünür. <c>Mobile</c> öneki mobil metinleri okur: <c>{l:Tr Mobile.Retry}</c>.
/// </summary>
[ContentProperty(nameof(Path))]
[AcceptEmptyServiceProvider]
public sealed class TrExtension : IMarkupExtension<BindingBase>
{
    public string Path { get; set; } = "";

    public string? StringFormat { get; set; }

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
        => Path.StartsWith("Mobile.", StringComparison.Ordinal)
            ? new Binding(Path["Mobile.".Length..], source: MobileText.Instance, stringFormat: StringFormat)
            : new Binding("S." + Path, source: Localizer.Instance, stringFormat: StringFormat);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}

/// <summary>Mobil metinlerin bağlama kaynağı; dil değişince tüm özellikler yeniden okunur.</summary>
public sealed class MobileText : System.ComponentModel.INotifyPropertyChanged
{
    public static MobileText Instance { get; } = new();

    private MobileText() => Lang.Changed += (_, _) =>
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(string.Empty));

    public string ReminderLocalHint => MobileStrings.Instance.ReminderLocalHint;
    public string ServerAddress => MobileStrings.Instance.ServerAddress;
    public string ServerAddressHint => MobileStrings.Instance.ServerAddressHint;
    public string ShareInvite => MobileStrings.Instance.ShareInvite;
    public string Retry => MobileStrings.Instance.Retry;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}
