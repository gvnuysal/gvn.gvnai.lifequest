using LifeQuest.Mobile.Core.ViewModels;

namespace LifeQuest.Mobile.Views;

public partial class PartyPage : LoadingPage, IQueryAttributable
{
    private readonly PartyViewModel _vm;

    public PartyPage(PartyViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _vm = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("code", out var code)) _vm.Code = Convert.ToString(code) ?? "";
    }

    protected override Task OnLoadAsync() => _vm.LoadCommand.ExecuteAsync(null);
}
