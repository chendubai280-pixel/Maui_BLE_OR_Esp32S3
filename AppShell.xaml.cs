using MauiApp2.Views;//命名空间引入Views文件夹

namespace MauiApp2
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));//注册新建的页面
            Routing.RegisterRoute(nameof(BlePage), typeof(BlePage));//注册其他新建的页面也是这样叠加
            Routing.RegisterRoute(nameof(ChatPage), typeof(ChatPage));
        }
    }
}
