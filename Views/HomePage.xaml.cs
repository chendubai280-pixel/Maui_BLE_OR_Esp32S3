using MauiApp2.Models;

namespace MauiApp2.Views;

public partial class HomePage : ContentPage
{
    public HomePageModel ViewModel { get; } = new HomePageModel();
    public HomePage()
    {
        InitializeComponent();
        BindingContext = ViewModel;//依赖注入

    }
    
}