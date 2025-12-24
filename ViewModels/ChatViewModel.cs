using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.ApplicationModel;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp2
{
    // 必须是 partial 类
    public partial class ChatViewModel : ObservableObject
    {
        private readonly ICharacteristic _characteristicTx; // 接收数据 (Notify)
        private readonly ICharacteristic _characteristicRx; // 发送数据 (Write)

        // 接收消息属性 (手动实现)
        private string _receivedMessage = "等待 ESP32 消息...";
        public string ReceivedMessage
        {
            get => _receivedMessage;
            set
            {
                if (_receivedMessage != value)
                {
                    _receivedMessage = value;
                    OnPropertyChanged(nameof(ReceivedMessage));
                }
            }
        }

        // LED 开关属性 (使用 CommunityToolkit.Mvvm 自动生成)
        [ObservableProperty]
        private bool _isLedOn;

        // 注意：[ObservableProperty] 已经自动生成了 partial void OnIsLedOnChanged(bool value); 的定义声明。
        // 因此，我们只需要提供实现即可。

        public ChatViewModel(ICharacteristic tx, ICharacteristic rx)
        {
            _characteristicTx = tx;
            _characteristicRx = rx;
            _characteristicTx.ValueUpdated += OnCharacteristicValueUpdated;

            // 启动接收更新
            Task.Run(async () => await _characteristicTx.StartUpdatesAsync());
        }

        // 接收到实时数据时触发的事件处理方法
        private void OnCharacteristicValueUpdated(object? sender, CharacteristicUpdatedEventArgs e)
        {
            var bytes = e.Characteristic.Value;
            var message = Encoding.UTF8.GetString(bytes);

            // 切换到主线程进行 UI 更新和异步操作
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // 1. 更新 UI
                ReceivedMessage = message;

                // 2. 检查是否为心跳包，如果是，则发送确认 (ACK)
                if (message.StartsWith("Heartbeat:"))
                {
                    await SendAckAsync();
                    System.Diagnostics.Debug.WriteLine("[BLE ACK] Heartbeat received, sending 'OK' to stop it.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[BLE RX] Received non-heartbeat message: {message}");
                }
            });
        }

        // 发送 ACK 消息给 ESP32
        private async Task SendAckAsync()
        {
            if (_characteristicRx != null)
            {
                try
                {
                    string ackMessage = "OK";
                    byte[] ackBytes = Encoding.UTF8.GetBytes(ackMessage);
                    // 确保 WriteAsync 在后台线程执行
                    await Task.Run(() => _characteristicRx.WriteAsync(ackBytes));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BLE ACK Error] Failed to send OK: {ex.Message}");
                }
            }
        }

        // 根据开关状态发送控制命令
        private async Task SendControlCommand(bool turnOn)
        {
            if (_characteristicRx != null)
            {
                try
                {
                    string command = turnOn ? "on" : "off";
                    byte[] commandBytes = Encoding.UTF8.GetBytes(command);

                    // 确保 WriteAsync 在后台线程执行
                    await Task.Run(() => _characteristicRx.WriteAsync(commandBytes));
                    System.Diagnostics.Debug.WriteLine($"[BLE Control] Sent: {command}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BLE Control Error] Failed to send command: {ex.Message}");
                }
            }
        }

        // 建议添加一个清理方法，用于页面关闭时取消订阅
        public void Cleanup()
        {
            if (_characteristicTx != null)
            {
                _characteristicTx.ValueUpdated -= OnCharacteristicValueUpdated;
                // 尝试停止更新，虽然不是必需，但有助于节省资源
                Task.Run(async () => await _characteristicTx.StopUpdatesAsync());
            }
        }

        // 分部方法的实现声明 (保留此部分)
        partial void OnIsLedOnChanged(bool value)
        {
            // 业务逻辑
            // 由于 SendControlCommand 内部已经使用了 Task.Run，
            // 这里的 Task.Run 是可选的，但保留它以确保 SendControlCommand 始终在后台线程执行。
            Task.Run(() => SendControlCommand(value));
        }
    }
}
