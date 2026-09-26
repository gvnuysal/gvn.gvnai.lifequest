using LifeQuest.Mobile.Core.Services;
using LifeQuest.Mobile.Views;

namespace LifeQuest.Mobile;

public partial class AppShell : Shell
{
    static AppShell()
    {
        Routing.RegisterRoute(Routes.Quest, typeof(QuestPage));
        Routing.RegisterRoute(Routes.Suggest, typeof(SuggestPage));
        Routing.RegisterRoute(Routes.Saved, typeof(SavedPage));
        Routing.RegisterRoute(Routes.Ideas, typeof(IdeasPage));
        Routing.RegisterRoute(Routes.Party, typeof(PartyPage));
    }

    public AppShell() => InitializeComponent();
}
