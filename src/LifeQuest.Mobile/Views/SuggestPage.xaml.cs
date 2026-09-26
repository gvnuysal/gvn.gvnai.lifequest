using LifeQuest.Mobile.Core.ViewModels;

namespace LifeQuest.Mobile.Views;

public partial class SuggestPage : LoadingPage
{
    private readonly SuggestViewModel _vm;

    public SuggestPage(SuggestViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
        Controls.SegmentRow.Attach(DurationRow);
        Controls.SegmentRow.Attach(CostRow);
    }

    protected override Task OnLoadAsync()
    {
        _vm.InitCommand.Execute(null);
        return Task.CompletedTask;
    }
}
