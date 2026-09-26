using LifeQuest.Mobile.Core.ViewModels;

namespace LifeQuest.Mobile.Views;

public partial class TodayPage : ContentPage
{
    private readonly TodayViewModel _vm;

    public TodayPage(TodayViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    /// <summary>Sekmeye her dönüşte güncel durum (kabul/tamamlama sonrası) yeniden yüklenir.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.RefreshCommand.ExecuteAsync(null);
    }
}
