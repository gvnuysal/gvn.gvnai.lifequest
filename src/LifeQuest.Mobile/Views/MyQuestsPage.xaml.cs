using LifeQuest.Mobile.Core.ViewModels;

namespace LifeQuest.Mobile.Views;

public partial class MyQuestsPage : LoadingPage
{
    private readonly MyQuestsViewModel _vm;

    public MyQuestsPage(MyQuestsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    protected override Task OnLoadAsync() => _vm.RefreshCommand.ExecuteAsync(null);
}
