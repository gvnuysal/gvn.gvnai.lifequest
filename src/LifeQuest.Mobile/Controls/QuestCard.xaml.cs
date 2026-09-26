namespace LifeQuest.Mobile.Controls;

public partial class QuestCard : Border
{
    public static readonly BindableProperty ShowExplanationProperty =
        BindableProperty.Create(nameof(ShowExplanation), typeof(bool), typeof(QuestCard), true,
            propertyChanged: (b, _, n) => ((QuestCard)b).Why.IsVisible = (bool)n);

    public QuestCard() => InitializeComponent();

    public bool ShowExplanation { get => (bool)GetValue(ShowExplanationProperty); set => SetValue(ShowExplanationProperty, value); }
}
