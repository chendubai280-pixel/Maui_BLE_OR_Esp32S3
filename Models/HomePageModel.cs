using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MauiApp2.Models
{
    public class HomePageModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;


        private string _InputValue;
        public string InputValue
        {
            get { return _InputValue; }
            set {
                    if (_InputValue != value)//如果输入value值刷新
                    {
                        _InputValue = value;//重新赋值给属性
                        OnPropertyChanged(nameof(Greeting));//InputValue属性变化时通知到Greeting
                }
                }
        }

        public string Greeting => $"hello,{InputValue}";//label.text的模型由InputValue输入xo

        /// <summary>
        /// 通知方法的封装
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
