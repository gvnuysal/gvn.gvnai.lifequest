using LifeQuest.Mobile.Core.ViewModels;

namespace LifeQuest.Mobile.Controls;

/// <summary>TR / EN anahtarı (web: ui/language-switch). Profilde hesaba yazan model verilir; girişte kendi modelini kurar.</summary>
public sealed class LanguageSwitch : HorizontalStackLayout
{
    public static readonly BindableProperty ModelProperty =
        BindableProperty.Create(nameof(Model), typeof(LanguageSwitchViewModel), typeof(LanguageSwitch), null,
            propertyChanged: (b, _, n) => ((LanguageSwitch)b).Attach((LanguageSwitchViewModel?)n));

    private readonly Button _tr = Make("TR", "tr");
    private readonly Button _en = Make("EN", "en");
    private LanguageSwitchViewModel? _model;

    public LanguageSwitch()
    {
        Spacing = 4;
        Children.Add(_tr);
        Children.Add(_en);
        Loaded += (_, _) =>
        {
            if (_model is null && IPlatformApplication.Current?.Services.GetService<LanguageSwitchViewModel>() is { } own)
                Attach(own);
        };
    }

    public LanguageSwitchViewModel? Model { get => (LanguageSwitchViewModel?)GetValue(ModelProperty); set => SetValue(ModelProperty, value); }

    private void Attach(LanguageSwitchViewModel? model)
    {
        if (model is null) return;
        if (_model is not null) _model.PropertyChanged -= OnChanged;
        _model = model;
        _model.PropertyChanged += OnChanged;
        _tr.Command = _en.Command = model.SelectCommand;
        Refresh();
    }

    private void OnChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        Paint(_tr, _model?.IsTurkish == true);
        Paint(_en, _model?.IsEnglish == true);
    }

    private static Button Make(string text, string code) => new()
    {
        Text = text,
        CommandParameter = code,
        FontSize = 13,
        Padding = new Thickness(12, 4),
        MinimumHeightRequest = 34,
        CornerRadius = 17,
        MinimumWidthRequest = 48
    };

    private static void Paint(Button button, bool selected)
    {
        button.BackgroundColor = selected ? Palette.Get("PrimarySoft") : Colors.Transparent;
        button.TextColor = selected ? Palette.Get("PrimaryText") : Palette.Get("Ink3");
        button.BorderColor = selected ? Palette.Get("Primary") : Palette.Get("Line");
        button.BorderWidth = 1.5;
    }
}
