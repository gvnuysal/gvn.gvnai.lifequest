using Microsoft.Maui.Controls.Shapes;

namespace LifeQuest.Mobile.Controls;

/// <summary>Kategori ikonu: renkli yumuşak zemin üzerinde kategori ikonu (web: ui/category-icon).</summary>
public sealed class CategoryIcon : Border
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(CategoryIcon), "compass", propertyChanged: (b, _, _) => ((CategoryIcon)b).Update());

    public static readonly BindableProperty ColorKeyProperty =
        BindableProperty.Create(nameof(ColorKey), typeof(string), typeof(CategoryIcon), "Explorer", propertyChanged: (b, _, _) => ((CategoryIcon)b).Update());

    public static readonly BindableProperty DiameterProperty =
        BindableProperty.Create(nameof(Diameter), typeof(double), typeof(CategoryIcon), 46d, propertyChanged: (b, _, _) => ((CategoryIcon)b).Update());

    private readonly IconView _icon = new() { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, Stroke = 2.2f };

    public CategoryIcon()
    {
        StrokeThickness = 0;
        Content = _icon;
        Update();
    }

    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string ColorKey { get => (string)GetValue(ColorKeyProperty); set => SetValue(ColorKeyProperty, value); }
    public double Diameter { get => (double)GetValue(DiameterProperty); set => SetValue(DiameterProperty, value); }

    private void Update()
    {
        WidthRequest = HeightRequest = Diameter;
        StrokeShape = new RoundRectangle { CornerRadius = Diameter * 0.32 };
        var color = Palette.Get("Cat" + ColorKey);
        BackgroundColor = color.WithAlpha(0.16f);
        _icon.Name = Icon;
        _icon.Color = color;
        _icon.Size = Diameter * 0.5;
    }
}

/// <summary>Boş/hata durumu (web: ui/empty-state): ikon, başlık, açıklama ve isteğe bağlı eylem.</summary>
public sealed class EmptyState : VerticalStackLayout
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(EmptyState), "info", propertyChanged: (b, _, n) => ((EmptyState)b)._icon.Name = (string)n);

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(EmptyState), "", propertyChanged: (b, _, n) => ((EmptyState)b)._title.Text = (string)n);

    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(EmptyState), null, propertyChanged: (b, _, n) =>
        {
            var view = (EmptyState)b;
            view._message.Text = n as string;
            view._message.IsVisible = !string.IsNullOrWhiteSpace(n as string);
        });

    public static readonly BindableProperty ActionTextProperty =
        BindableProperty.Create(nameof(ActionText), typeof(string), typeof(EmptyState), null, propertyChanged: (b, _, n) =>
        {
            var view = (EmptyState)b;
            view._action.Text = n as string;
            view._action.IsVisible = !string.IsNullOrWhiteSpace(n as string);
        });

    public static readonly BindableProperty ActionCommandProperty =
        BindableProperty.Create(nameof(ActionCommand), typeof(System.Windows.Input.ICommand), typeof(EmptyState), null,
            propertyChanged: (b, _, n) => ((EmptyState)b)._action.Command = n as System.Windows.Input.ICommand);

    private readonly IconView _icon = new() { Size = 36, HorizontalOptions = LayoutOptions.Center };
    private readonly Label _title = new() { HorizontalTextAlignment = TextAlignment.Center, FontFamily = "NunitoExtraBold", FontSize = 18 };
    private readonly Label _message = new() { HorizontalTextAlignment = TextAlignment.Center, IsVisible = false };
    private readonly Button _action = new() { IsVisible = false, HorizontalOptions = LayoutOptions.Center };

    public EmptyState()
    {
        Spacing = 10;
        Padding = new Thickness(16, 28);
        _icon.SetAppThemeColor(IconView.ColorProperty, Palette.Raw("Ink3Light"), Palette.Raw("Ink3Dark"));
        _message.SetAppThemeColor(Label.TextColorProperty, Palette.Raw("Ink2Light"), Palette.Raw("Ink2Dark"));
        if (Application.Current?.Resources.TryGetValue("Secondary", out var style) == true) _action.Style = (Style)style;
        Children.Add(_icon);
        Children.Add(_title);
        Children.Add(_message);
        Children.Add(_action);
    }

    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Message { get => (string?)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public string? ActionText { get => (string?)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }
    public System.Windows.Input.ICommand? ActionCommand { get => (System.Windows.Input.ICommand?)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }
}

/// <summary>
/// Alttan açılan sayfa (web: ui/sheet). Sayfanın en üst katmanına konur; <see cref="IsOpen"/> ile açılır, dışarı
/// dokununca kapanır.
/// </summary>
[ContentProperty(nameof(SheetContent))]
public sealed class Sheet : Grid
{
    public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(Sheet), false, BindingMode.TwoWay,
            propertyChanged: (b, _, n) => ((Sheet)b).IsVisible = (bool)n);

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(Sheet), "", propertyChanged: (b, _, n) => ((Sheet)b)._title.Text = (string)n);

    public static readonly BindableProperty SheetContentProperty =
        BindableProperty.Create(nameof(SheetContent), typeof(View), typeof(Sheet), null, propertyChanged: (b, _, n) => ((Sheet)b)._body.Content = (View?)n);

    private readonly Label _title = new() { FontFamily = "NunitoExtraBold", FontSize = 20, VerticalOptions = LayoutOptions.Center };
    private readonly ContentView _body = new();

    public Sheet()
    {
        IsVisible = false;
        var dim = new BoxView { Color = Color.FromRgba(20, 16, 28, 0.45) };
        dim.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => IsOpen = false) });
        Children.Add(dim);

        var closeIcon = new IconView { Name = "x", Size = 22, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center };
        var closeTap = new TapGestureRecognizer { Command = new Command(() => IsOpen = false) };
        closeIcon.GestureRecognizers.Add(closeTap);
        closeIcon.InputTransparent = false;
        var header = new Grid { ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)] };
        header.Add(_title);
        header.Add(closeIcon, 1);

        var panel = new Border
        {
            VerticalOptions = LayoutOptions.End,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(28, 28, 0, 0) },
            Padding = new Thickness(20, 16, 20, 32),
            MaximumHeightRequest = 640,
            Content = new ScrollView { Content = new VerticalStackLayout { Spacing = 14, Children = { header, _body } } }
        };
        panel.SetAppThemeColor(BackgroundColorProperty, Palette.Raw("SurfaceLight"), Palette.Raw("SurfaceDark"));
        Children.Add(panel);
    }

    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public View? SheetContent { get => (View?)GetValue(SheetContentProperty); set => SetValue(SheetContentProperty, value); }
}
