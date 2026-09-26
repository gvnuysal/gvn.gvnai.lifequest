namespace LifeQuest.Mobile.Views;

/// <summary>Sayfa açıldığında ViewModel komutunu çalıştıran taban (ilk yükleme ve sekmeye dönüşte yenileme).</summary>
public abstract class LoadingPage : ContentPage
{
    protected abstract Task OnLoadAsync();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await OnLoadAsync();
    }
}
