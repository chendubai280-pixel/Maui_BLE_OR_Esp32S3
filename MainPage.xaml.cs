using MauiApp2.Views;

namespace MauiApp2
{
    public partial class MainPage : ContentPage
    {
       

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object? sender, EventArgs e)
        {
            Shell.Current.GoToAsync(nameof(BlePage));//跳转到home页面
        }

    }
}
