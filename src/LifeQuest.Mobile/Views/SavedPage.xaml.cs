using LifeQuest.Mobile.Core.ViewModels;

namespace LifeQuest.Mobile.Views;

public partial class SavedPage : LoadingPage
{
    private readonly SavedViewModel _vm;

    public SavedPage(SavedViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    protected override Task OnLoadAsync() => _vm.RefreshCommand.ExecuteAsync(null);

    private async void OnBack(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
