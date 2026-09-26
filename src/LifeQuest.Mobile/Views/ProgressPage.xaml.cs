using LifeQuest.Mobile.Core.ViewModels;

namespace LifeQuest.Mobile.Views;

public partial class ProgressPage : LoadingPage
{
    private readonly ProgressViewModel _vm;

    public ProgressPage(ProgressViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    protected override Task OnLoadAsync() => _vm.RefreshCommand.ExecuteAsync(null);
}
