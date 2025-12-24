using CommunityToolkit.Mvvm.ComponentModel;
using MauiApp2.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
namespace MauiApp2.ViewModels
{
    public class MainViewModel : ObservableObject // 继承MVVM类
    {
        public ObservableCollection<Person> People { get; set; }//页面绑定的list，用Person类处理json数据
        private Person _selectedPerson;
        public Person SelectedPerson //存放被点击事件成员
        {
            get => _selectedPerson;
            set => SetProperty(ref _selectedPerson, value);
        }

        public ICommand SelectionChangedCommand { get; }//页面列表点击事件绑定接口

        public MainViewModel()
        {
            // ... 构建点击事件接口，并将点击事件委托给自定义函数OnSelectionChanged进行处理...
            SelectionChangedCommand = new Command(OnSelectionChanged);

            //控制器类构建时先新建一些模型数据
            People = new ObservableCollection<Person>
             {
                 new Person { Name = "张三", Age = 30, City = "北京" },
                 new Person { Name = "李四", Age = 25, City = "上海" },
                 new Person { Name = "王五", Age = 40, City = "广州" },
             };

        }


        /// <summary>
        /// 自定义的处理程序
        /// </summary>
        /// <param name="selectedItem"></param>
        private void OnSelectionChanged(object selectedItem)//传入被点击的list成员selectedItem
        {
            if (selectedItem is Person person)
            {
                // 处理选择逻辑，例如显示一个 Alert
                Application.Current.MainPage.DisplayAlert("选中项", $"您选择了: {person.Name}", "OK");
            }
        }
    }

}
