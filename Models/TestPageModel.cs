using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;//引入MVVM包委托类命名空间
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;//引入系统自带事件委托空间
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace MauiApp2.Models
{
    public partial class TestPageModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(lanbel_text))]
        private string test;
        public string lanbel_text => $"hello{Test}";
        public IAsyncRelayCommand click_open { get; } //异步委托IAsyncRelayCommand
        public TestPageModel()
        {
            //传参<string>类型
            click_open = new AsyncRelayCommand<string>(RestButton); ; //构建异步传参委托
        }
        int i = 0;
        //自定义的方法
        private async Task RestButton(string str)//接收传参
        {
            Test = "ok";
            test = "手动异步命令执行中...";
            try
            {
                await Task.Delay(2000); 
                i++;
                Test = $"手动异步命令完成。点击次数: {str}";
            }

            catch (Exception ex)
            {
            }
        }
    }
}
