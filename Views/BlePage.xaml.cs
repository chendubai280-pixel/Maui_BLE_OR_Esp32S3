using MauiApp2.ViewModels;

namespace MauiApp2.Views;

public partial class BlePage : ContentPage
{
	public BlePage()
	{
		InitializeComponent();
        BindingContext = new BleViewModel();
    }
}