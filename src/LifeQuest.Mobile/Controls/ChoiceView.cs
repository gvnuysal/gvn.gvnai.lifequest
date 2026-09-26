using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace LifeQuest.Mobile.Controls;

public enum ChoiceKind
{
    /// <summary>Hap biçimli küçük seçim (web: ui/chip).</summary>
    Chip,

    /// <summary>İkon, başlık ve açıklamalı kart (web: ui/option-card).</summary>
    Option,

    /// <summary>Segment düğmesi (web: ui/segmented); eşit genişlikte yan yana.</summary>
    Segment
}

/// <summary>Seçilebilir çip / seçenek kartı / segment. Seçili durumda birincil renkle vurgulanır.</summary>
public sealed class ChoiceView : Border
{
    public static readonly BindableProperty TextProperty = Create<string>(nameof(Text), "");
    public static readonly BindableProperty HintProperty = Create<string?>(nameof(Hint), null);
    public static readonly BindableProperty IconProperty = Create<string?>(nameof(Icon), null);
    public static readonly BindableProperty ColorKeyProperty = Create<string?>(nameof(ColorKey), null);
    public static readonly BindableProperty IsSelectedProperty = Create<bool>(nameof(IsSelected), false);
    public static readonly BindableProperty KindProperty = Create<ChoiceKind>(nameof(Kind), ChoiceKind.Chip);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ChoiceView));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(ChoiceView));

    private readonly Label _text = new() { FontFamily = "NunitoBold", VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label _hint = new() { FontSize = 13, IsVisible = false };
    private readonly IconView _icon = new() { IsVisible = false, VerticalOptions = LayoutOptions.Center };
    private ChoiceKind? _builtKind;
    private readonly IconView _check = new() { Name = "check", Size = 18, Stroke = 2.6f, IsVisible = false, VerticalOptions = LayoutOptions.Center };

    public ChoiceView()
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (Command?.CanExecute(CommandParameter) == true) Command.Execute(CommandParameter);
        };
        GestureRecognizers.Add(tap);
        SemanticProperties.SetHint(this, "");
        Render();
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? ColorKey { get => (string?)GetValue(ColorKeyProperty); set => SetValue(ColorKeyProperty, value); }
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public ChoiceKind Kind { get => (ChoiceKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    private static BindableProperty Create<T>(string name, T fallback)
        => BindableProperty.Create(name, typeof(T), typeof(ChoiceView), fallback, propertyChanged: (b, _, _) => ((ChoiceView)b).Render());

    private void Render()
    {
        _text.Text = Text;
        _hint.Text = Hint;
        _hint.IsVisible = Kind == ChoiceKind.Option && !string.IsNullOrWhiteSpace(Hint);
        _icon.Name = Icon ?? "info";
        _icon.IsVisible = Icon is not null;
        _check.IsVisible = Kind == ChoiceKind.Option && IsSelected;

        var accent = ColorKey is null ? Palette.Get("Primary") : Palette.Get("Cat" + ColorKey);
        StrokeThickness = 1.5;
        Stroke = IsSelected ? accent : Palette.Get("Line");
        BackgroundColor = IsSelected
            ? (ColorKey is null ? Palette.Get("PrimarySoft") : accent.WithAlpha(0.14f))
            : Palette.Get("Surface");
        _text.TextColor = IsSelected && Kind != ChoiceKind.Option ? Palette.Get("PrimaryText") : Palette.Get("Ink");
        _hint.TextColor = Palette.Get("Ink2");
        _icon.Color = ColorKey is null ? (IsSelected ? Palette.Get("PrimaryText") : Palette.Get("Ink2")) : accent;
        _check.Color = accent;
        SemanticProperties.SetDescription(this, IsSelected ? $"{Text}, ✓" : Text);

        if (_builtKind == Kind) return;
        _builtKind = Kind;
        foreach (var view in new View[] { _text, _hint, _icon, _check })
            (view.Parent as Layout)?.Remove(view);

        switch (Kind)
        {
            case ChoiceKind.Option:
                StrokeShape = new RoundRectangle { CornerRadius = 14 };
                Padding = new Thickness(14, 12);
                _icon.Size = 24;
                _text.FontSize = 16;
                var grid = new Grid { ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)], ColumnSpacing = 12 };
                grid.Add(_icon);
                grid.Add(new VerticalStackLayout { Spacing = 2, Children = { _text, _hint } }, 1);
                grid.Add(_check, 2);
                Content = grid;
                break;
            case ChoiceKind.Segment:
                StrokeShape = new RoundRectangle { CornerRadius = 12 };
                Padding = new Thickness(4, 10);
                _text.FontSize = 14;
                _text.LineBreakMode = LineBreakMode.NoWrap;
                _text.MaxLines = 1;
                _text.HorizontalTextAlignment = TextAlignment.Center;
                _text.HorizontalOptions = LayoutOptions.Center;
                Content = _text;
                break;
            default:
                StrokeShape = new RoundRectangle { CornerRadius = 999 };
                Padding = new Thickness(14, 8);
                Margin = new Thickness(0, 0, 8, 8);
                _icon.Size = 16;
                _text.FontSize = 14;
                Content = new HorizontalStackLayout { Spacing = 6, Children = { _icon, _text } };
                break;
        }
    }
}
