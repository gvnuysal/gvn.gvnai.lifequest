using LifeQuest.Mobile.Core.ViewModels;

namespace LifeQuest.Mobile.Views;

public partial class OnboardingPage : ContentPage
{
    private readonly OnboardingViewModel _vm;

    public OnboardingPage(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private async void OnNextClicked(object? sender, EventArgs e) => await Scroller.ScrollToAsync(0, 0, false);

    /// <summary>Başlangıç kartı: sağa kaydır = bana göre, sola = bana göre değil.</summary>
    private void OnCardPan(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                StarterCard.TranslationX = e.TotalX;
                StarterCard.Rotation = e.TotalX / 20;
                break;
            case GestureStatus.Completed or GestureStatus.Canceled:
                var x = StarterCard.TranslationX;
                StarterCard.TranslationX = 0;
                StarterCard.Rotation = 0;
                if (x > 110) _vm.LikeCommand.Execute(null);
                else if (x < -110) _vm.DislikeCommand.Execute(null);
                break;
        }
    }
}
