using LifeQuest.Mobile.Core.Formatting;
using Microsoft.Maui.Graphics.Platform;
using IconShapeData = LifeQuest.Mobile.Core.Formatting.IconShape;

namespace LifeQuest.Mobile.Controls;

/// <summary>
/// Web ikon setindeki 24×24 çizgi ikonu (ui/icon.ts). SVG yolu 24'lük kutuya göre ölçeklenir; renk ve çizgi kalınlığı
/// ayarlanabilir.
/// </summary>
public sealed class IconView : GraphicsView, IDrawable
{
    public static readonly BindableProperty NameProperty =
        BindableProperty.Create(nameof(Name), typeof(string), typeof(IconView), "info", propertyChanged: Redraw);

    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create(nameof(Color), typeof(Color), typeof(IconView), Colors.Gray, propertyChanged: Redraw);

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(IconView), 20d, propertyChanged: (b, _, n) =>
        {
            var view = (IconView)b;
            view.WidthRequest = view.HeightRequest = (double)n;
        });

    public static readonly BindableProperty StrokeProperty =
        BindableProperty.Create(nameof(Stroke), typeof(float), typeof(IconView), 2f, propertyChanged: Redraw);

    private static readonly Dictionary<string, PathF> Cache = [];

    public IconView()
    {
        Drawable = this;
        WidthRequest = HeightRequest = 20;
        InputTransparent = true;
        BackgroundColor = Colors.Transparent;
    }

    public string Name { get => (string)GetValue(NameProperty); set => SetValue(NameProperty, value); }
    public Color Color { get => (Color)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public float Stroke { get => (float)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    public void Draw(ICanvas canvas, RectF rect)
    {
        var shape = Icons.Get(Name);
        if (!Cache.TryGetValue(shape.Data, out var path))
            Cache[shape.Data] = path = PathBuilder.Build(shape.Data);

        var scale = Math.Min(rect.Width, rect.Height) / 24f;
        canvas.SaveState();
        canvas.Translate(rect.X + (rect.Width - 24 * scale) / 2, rect.Y + (rect.Height - 24 * scale) / 2);
        canvas.Scale(scale, scale);
        if (shape.Rotated) canvas.Rotate(180, 12, 12);
        canvas.StrokeColor = Color;
        canvas.StrokeSize = Stroke;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(path);
        canvas.RestoreState();
    }

    private static void Redraw(BindableObject bindable, object oldValue, object newValue) => ((IconView)bindable).Invalidate();
}

/// <summary>Yuvarlak uçlu ilerleme çubuğu (web: ui/progress-bar).</summary>
public sealed class BarView : GraphicsView, IDrawable
{
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(double), typeof(BarView), 0d, propertyChanged: (b, _, _) => ((BarView)b).Invalidate());

    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create(nameof(Color), typeof(Color), typeof(BarView), Colors.Orange, propertyChanged: (b, _, _) => ((BarView)b).Invalidate());

    public static readonly BindableProperty TrackColorProperty =
        BindableProperty.Create(nameof(TrackColor), typeof(Color), typeof(BarView), Colors.LightGray, propertyChanged: (b, _, _) => ((BarView)b).Invalidate());

    public BarView()
    {
        Drawable = this;
        HeightRequest = 8;
        InputTransparent = true;
    }

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public Color Color { get => (Color)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public Color TrackColor { get => (Color)GetValue(TrackColorProperty); set => SetValue(TrackColorProperty, value); }

    public void Draw(ICanvas canvas, RectF rect)
    {
        var radius = rect.Height / 2;
        canvas.FillColor = TrackColor;
        canvas.FillRoundedRectangle(rect, radius);
        var width = (float)Math.Clamp(Value, 0, 1) * rect.Width;
        if (width <= 0) return;
        canvas.FillColor = Color;
        canvas.FillRoundedRectangle(new RectF(rect.X, rect.Y, Math.Max(width, rect.Height), rect.Height), radius);
    }
}

/// <summary>Seviye halkası (web: ui/level-ring): ortada seviye, çevrede ilerleme yayı.</summary>
public sealed class LevelRingView : GraphicsView, IDrawable
{
    public static readonly BindableProperty LevelProperty =
        BindableProperty.Create(nameof(Level), typeof(int), typeof(LevelRingView), 1, propertyChanged: (b, _, _) => ((LevelRingView)b).Invalidate());

    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(double), typeof(LevelRingView), 0d, propertyChanged: (b, _, _) => ((LevelRingView)b).Invalidate());

    public LevelRingView()
    {
        Drawable = this;
        WidthRequest = HeightRequest = 132;
        InputTransparent = true;
    }

    public int Level { get => (int)GetValue(LevelProperty); set => SetValue(LevelProperty, value); }
    public double Progress { get => (double)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }

    /// <summary>Halka altındaki "Seviye" etiketi.</summary>
    public string Caption { get; set; } = "LEVEL";

    public void Draw(ICanvas canvas, RectF rect)
    {
        var size = Math.Min(rect.Width, rect.Height);
        const float stroke = 12;
        var box = new RectF(rect.Center.X - size / 2 + stroke / 2, rect.Center.Y - size / 2 + stroke / 2, size - stroke, size - stroke);

        canvas.StrokeSize = stroke;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeColor = Palette.Get("Surface3");
        canvas.DrawEllipse(box);

        var sweep = (float)Math.Clamp(Progress, 0, 1) * 360;
        if (sweep > 0)
        {
            canvas.StrokeColor = Palette.Get("Xp");
            canvas.DrawArc(box, 90, 90 - sweep, true, false);
        }

        canvas.FontColor = Palette.Get("Ink");
        canvas.Font = new Microsoft.Maui.Graphics.Font("Nunito-Black");
        canvas.FontSize = size * 0.3f;
        canvas.DrawString(Level.ToString(), rect, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}

/// <summary>Kutlama konfetisi: renkli parçacıklar düşer (web: quest/celebration).</summary>
public sealed class ConfettiView : GraphicsView, IDrawable
{
    private static readonly string[] ColorKeys = ["CatExplorer", "CatCulture", "CatLearning", "CatSocial", "CatFitness", "CatCreativity", "Xp", "Brand"];
    private readonly List<(float X, float Speed, float Size, float Spin, string Color)> _pieces = [];
    private float _t;

    public ConfettiView()
    {
        Drawable = this;
        InputTransparent = true;
        var random = new Random(7);
        for (var i = 0; i < 28; i++)
            _pieces.Add(((float)random.NextDouble(), 0.6f + (float)random.NextDouble(), 6 + random.Next(6), random.Next(360), ColorKeys[i % ColorKeys.Length]));
    }

    public void Start()
    {
        _t = 0;
        this.Animate("confetti", v => { _t = (float)v; Invalidate(); }, 0, 1, length: 1600, easing: Easing.CubicOut);
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        if (_t is <= 0 or >= 1) return;
        foreach (var p in _pieces)
        {
            canvas.SaveState();
            canvas.Alpha = 1 - _t;
            canvas.FillColor = Palette.Get(p.Color);
            var x = rect.X + p.X * rect.Width;
            var y = rect.Y + (-0.1f + _t * p.Speed) * rect.Height;
            canvas.Rotate(p.Spin + _t * 360, x, y);
            canvas.FillRectangle(x, y, p.Size, p.Size * 0.5f);
            canvas.RestoreState();
        }
    }
}
