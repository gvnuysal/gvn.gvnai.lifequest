using System.ComponentModel;
using LifeQuest.Mobile.Core.ViewModels;

namespace LifeQuest.Mobile.Views;

public partial class QuestPage : LoadingPage, IQueryAttributable
{
    private readonly QuestViewModel _vm;

    public QuestPage(QuestViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
        _vm.PropertyChanged += OnViewModelChanged;
    }

    /// <summary>Rota parametresi: <c>quest?id=…</c> (Guid ya da metin).</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("id", out var value)) return;
        _vm.Id = value switch
        {
            Guid id => id,
            string text when Guid.TryParse(text, out var parsed) => parsed,
            _ => _vm.Id
        };
    }

    protected override Task OnLoadAsync() => _vm.LoadCommand.ExecuteAsync(null);

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(QuestViewModel.WhyOpen))
            _ = WhyChevron.RotateToAsync(_vm.WhyOpen ? 90 : 0, 150);

        // Kutlama açılınca: konfeti ve XP sayacı (web: ease-out, 900 ms).
        if (e.PropertyName == nameof(QuestViewModel.Celebration) && _vm.Celebration is { } celebration)
        {
            Confetti.Start();
            this.Animate("xp", celebration.Animate, 0, 1, length: 900, finished: (_, _) => celebration.Animate(1));
        }
    }
}
