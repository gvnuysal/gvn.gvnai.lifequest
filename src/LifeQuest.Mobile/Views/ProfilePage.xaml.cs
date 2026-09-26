using LifeQuest.Mobile.Core.ViewModels;

namespace LifeQuest.Mobile.Views;

public partial class ProfilePage : LoadingPage
{
    private readonly ProfileViewModel _vm;

    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
        foreach (var row in new[] { RadiusRow, BudgetRow, TimeRow, EffortRow, NotificationRow, ThemeRow })
            Controls.SegmentRow.Attach(row);
    }

    protected override Task OnLoadAsync() => _vm.RefreshCommand.ExecuteAsync(null);
}
