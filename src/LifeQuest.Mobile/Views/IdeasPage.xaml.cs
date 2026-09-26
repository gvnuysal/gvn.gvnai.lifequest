using LifeQuest.Mobile.Core.ViewModels;

namespace LifeQuest.Mobile.Views;

public partial class IdeasPage : LoadingPage
{
    private readonly IdeasViewModel _vm;

    public IdeasPage(IdeasViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    protected override Task OnLoadAsync() => _vm.RefreshCommand.ExecuteAsync(null);

    private async void OnBack(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
