using MauiApp2.ViewModels;
using Plugin.BLE.Abstractions.Contracts; // 引入 ICharacteristic 所需的命名空间

namespace MauiApp2.Views
{
    public partial class ChatPage : ContentPage
    {
        // 关键修改：构造函数需要接收 ICharacteristic 参数
        // 这些参数应该由上一个页面（例如连接页面）在导航时传递过来
        public ChatPage(ICharacteristic tx, ICharacteristic rx)
        {
            InitializeComponent();

            // 使用接收到的参数初始化 ChatViewModel
            BindingContext = new ChatViewModel(tx, rx);
        }

        // 当页面被关闭时，进行清理
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (BindingContext is ChatViewModel vm)
            {
                vm.Cleanup();
            }
        }

        // 如果您希望在返回键被按下时也进行清理，可以重写 OnNavigatedFrom
        protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            if (BindingContext is ChatViewModel vm)
            {
                vm.Cleanup();
            }
        }
    }
}
