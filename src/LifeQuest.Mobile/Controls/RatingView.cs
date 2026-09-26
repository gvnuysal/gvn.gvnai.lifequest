using LifeQuest.Mobile.Core.Localization;

namespace LifeQuest.Mobile.Controls;

/// <summary>1–5 yıldız puanı (web: ui/rating). Aynı yıldıza tekrar dokunmak puanı kaldırır.</summary>
public sealed class RatingView : HorizontalStackLayout
{
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(int), typeof(RatingView), 0, BindingMode.TwoWay,
            propertyChanged: (b, _, _) => ((RatingView)b).Paint());

    private readonly List<IconView> _stars = [];

    public RatingView()
    {
        Spacing = 6;
        for (var i = 1; i <= 5; i++)
        {
            var value = i;
            var star = new IconView { Name = "star", Size = 34, Stroke = 2, InputTransparent = false };
            star.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Value = Value == value ? 0 : value) });
            SemanticProperties.SetDescription(star, Localizer.Instance.S.Ui.Stars(value));
            _stars.Add(star);
            Children.Add(star);
        }

        Paint();
    }

    public int Value { get => (int)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    private void Paint()
    {
        for (var i = 0; i < _stars.Count; i++)
            _stars[i].Color = i < Value ? Palette.Get("Xp") : Palette.Get("Ink3");
    }
}
