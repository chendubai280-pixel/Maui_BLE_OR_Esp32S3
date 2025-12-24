using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp2.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MauiApp2
{

    public partial class BleViewModel : ObservableObject
    {

        // 定义 BLE 服务和特征的 GUID
        private static readonly Guid ServiceUuid = new Guid("4fafc201-1fb5-459e-8fcc-c4c9c331914b");

        // 用于接收数据 (ESP32 -> APP) 的特征 (Notify)
        private static readonly Guid CharacteristicTxUuid = new Guid("beb5483e-36e1-4688-b7f5-ea07361b26a8");

        // 用于发送数据 (APP -> ESP32) 的特征 (Write)
        private static readonly Guid CharacteristicRxUuid = new Guid("ae5b0185-3e28-4856-91e0-e14b533a0129");


        // 定义 BLE 操作的枚举状态
        public enum BleState
        {
            Idle,       // 空闲状态
            Scanning,   // 正在扫描
            Connecting, // 正在连接
            Connected,  // 已连接
        }


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ButtonText))]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        private BleState state;


        private readonly IBluetoothLE _bluetoothLe; // 蓝牙核心接口实例
        private IDevice? _pairedDevice;             // 存储当前尝试连接或已连接的设备实例

        // 新增：存储特征实例
        private ICharacteristic? _characteristicTx; // 存储 TX (Notify) 特征实例
        private ICharacteristic? _characteristicRx; // 存储 RX (Write) 特征实例


        // 发现的附近蓝牙，使用 ObservableCollection 确保 UI 自动更新
        public ObservableCollection<IDevice> DiscoveredDevices { get; } = [];


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


        // 绑定到 UI 按钮的命令
        public ICommand ActionCommand { get; }


        // 根据当前 State 返回状态文本，用于 UI 显示
        public string StatusText => State switch
        {
            BleState.Scanning => "Scanning...",
            BleState.Connecting => "Connecting...",
            BleState.Connected => "Connected",
            _ => "" // Idle 或其他状态显示为空
        };

        // 根据当前 State 和 SelectedDevice 返回按钮文本，用于 UI 显示
        public string ButtonText => State switch
        {
            BleState.Idle when SelectedDevice == null => "Start Scan",
            BleState.Scanning when SelectedDevice == null => "Stop Scan",
            BleState.Connecting => "Connecting...",
            BleState.Connected => "Disconnect",
            _ => "Connect"
        };

        // 选中的设备属性
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ButtonText))]
        private IDevice? selectedDevice;


        // 构造函数
        public BleViewModel()
        {
            _bluetoothLe = CrossBluetoothLE.Current;
            _bluetoothLe.Adapter.ScanTimeout = 30000;

            _bluetoothLe.StateChanged += OnBleStateChanged;
            _bluetoothLe.Adapter.DeviceDiscovered += OnDeviceDiscovered;
            _bluetoothLe.Adapter.DeviceConnected += OnDeviceConnected;
            _bluetoothLe.Adapter.DeviceDisconnected += OnDeviceDisconnected;
            _bluetoothLe.Adapter.DeviceConnectionLost += OnDeviceDisconnected;

            ActionCommand = new AsyncRelayCommand(DoActionAsync);
        }

        /// <summary>
        /// 蓝牙状态变化事件处理
        /// </summary>
        private async void OnBleStateChanged(object? sender, BluetoothStateChangedArgs args)
        {
            if (args.OldState == BluetoothState.On && args.NewState != BluetoothState.On)
            {
                State = BleState.Connecting;
            }
            else if (args.OldState != BluetoothState.On && args.NewState == BluetoothState.On)
            {
                await Connect();
            }
        }


        /// <summary>
        /// 发现设备事件处理
        /// </summary>
        private void OnDeviceDiscovered(object? sender, DeviceEventArgs args)
        {
            if (State != BleState.Scanning || DiscoveredDevices.Any(d => d.Id == args.Device.Id))
            {
                return;
            }
            DiscoveredDevices.Add(args.Device);
        }

        // BleViewModel.cs -> OnDeviceConnected 方法中

        private async void OnDeviceConnected(object? sender, DeviceEventArgs args)
        {
            // 立即切换到主线程来执行所有后续的 UI 相关操作（包括导航和状态更新）
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // 状态更新必须在主线程
                State = BleState.Connected;

                ICharacteristic? characteristicTx = null;
                ICharacteristic? characteristicRx = null;

                try
                {
                    // ... (省略了获取 Service 和 Characteristic 的代码，保持不变) ...
                    var service = await args.Device.GetServiceAsync(ServiceUuid);
                    if (service == null) { /* ... */ return; }

                    characteristicTx = await service.GetCharacteristicAsync(CharacteristicTxUuid);
                    if (characteristicTx == null || !characteristicTx.CanUpdate) { /* ... */ return; }

                    characteristicRx = await service.GetCharacteristicAsync(CharacteristicRxUuid);
                    if (characteristicRx == null || !characteristicRx.CanWrite) { /* ... */ }

                    // ------------------------------------------------------------------
                    // 4. *** 关键修改：手动创建 ChatPage 实例并使用 PushAsync 导航 ***
                    // ------------------------------------------------------------------

                    // 确保 characteristicTx 和 characteristicRx 不为 null (如果为 null，则传递 null，ChatViewModel 内部需要处理)
                    // 考虑到您前面的检查，这里通常是有效的对象。

                    // 1. 手动创建 ChatPage 实例，并传入特征值对象
                    var chatPage = new ChatPage(characteristicTx!, characteristicRx!);

                    // 2. 使用导航堆栈推送页面
                    // 注意：PushAsync 不使用路由名称，而是直接使用页面实例
                    await Shell.Current.Navigation.PushAsync(chatPage);

                    // 导航成功后，将状态设为 Idle
                    State = BleState.Idle;

                }
                catch (Exception e)
                {
                    await ShowAlert("Error", $"Exception during connection setup: {e.Message}");
                    State = BleState.Idle;
                }
            }); // 结束 MainThread.InvokeOnMainThreadAsync
        }


        // 接收到实时数据时触发的事件处理方法 - **已更新**：发送 ACK
        private void OnCharacteristicValueUpdated(object? sender, CharacteristicUpdatedEventArgs e)
        {
            var bytes = e.Characteristic.Value;
            var message = System.Text.Encoding.UTF8.GetString(bytes);

            // 必须使用 MainThread.BeginInvokeOnMainThread 来更新 UI 绑定的属性
            // 注意：lambda 表达式改为 async，以便在主线程上执行 WriteAsync
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // 1. 更新 UI
                ReceivedMessage = message;

                // 2. *** 发送确认 (ACK) ***
                if (_characteristicRx != null)
                {
                    try
                    {
                        string ackMessage = "OK";
                        byte[] ackBytes = System.Text.Encoding.UTF8.GetBytes(ackMessage);

                        // 写入数据到 ESP32 的 RX 特征
                        await _characteristicRx.WriteAsync(ackBytes);
                        System.Diagnostics.Debug.WriteLine($"[BLE ACK] Sent: {ackMessage} to ESP32.");
                    }
                    catch (Exception ex)
                    {
                        // 写入失败，记录日志
                        System.Diagnostics.Debug.WriteLine($"[BLE ACK Error] Failed to send OK: {ex.Message}");
                    }
                }
            });
        }

        /// <summary>
        /// 设备断开连接或连接丢失事件处理 - **已更新**：清理特征
        /// </summary>
        private async void OnDeviceDisconnected(object? sender, DeviceEventArgs e)
        {
            // 只有当状态为已连接且存在已配对设备时才执行重连逻辑
            if (State != BleState.Connected || _pairedDevice == null)
            {
                // 清理特征和订阅 (即使没有重连，也要清理)
                if (_characteristicTx != null)
                {
                    _characteristicTx.ValueUpdated -= OnCharacteristicValueUpdated;
                    await _characteristicTx.StopUpdatesAsync();
                }
                _characteristicTx = null;
                _characteristicRx = null;

                // 如果是正常断开，则直接返回
                if (State == BleState.Connected)
                {
                    State = BleState.Idle;
                    return;
                }

                return;
            }

            // 清理特征和订阅
            if (_characteristicTx != null)
            {
                _characteristicTx.ValueUpdated -= OnCharacteristicValueUpdated;
                await _characteristicTx.StopUpdatesAsync();
            }
            _characteristicTx = null;
            _characteristicRx = null;


            // 设置状态为连接中，并尝试重新连接
            State = BleState.Connecting;
            await Connect();
        }


        /// <summary>
        /// 核心动作执行方法，根据当前状态决定执行的操作
        /// </summary>
        private async Task DoActionAsync()
        {
            switch (State)
            {
                case BleState.Idle when SelectedDevice == null:
                    await StartScanning();
                    break;

                case BleState.Scanning when SelectedDevice == null:
                    State = BleState.Idle;
                    await _bluetoothLe.Adapter.StopScanningForDevicesAsync();
                    break;

                case BleState.Idle when SelectedDevice != null:
                case BleState.Scanning when SelectedDevice != null:
                    await _bluetoothLe.Adapter.StopScanningForDevicesAsync();
                    _pairedDevice = SelectedDevice;
                    await Connect();
                    break;

                case BleState.Connected:
                    State = BleState.Idle;
                    await _bluetoothLe.Adapter.DisconnectDeviceAsync(_pairedDevice);
                    SelectedDevice = null;
                    _pairedDevice = null;
                    break;
            }
        }


        /// <summary>
        /// 开始扫描设备
        /// </summary>
        private async Task StartScanning()
        {
            DiscoveredDevices.Clear();

            var status = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Bluetooth>();
            }

            if (status != PermissionStatus.Granted)
            {
                await ShowAlert("Error", "No BLE permissions!");
                return;
            }

            State = BleState.Scanning;

            _bluetoothLe.Adapter
                .StartScanningForDevicesAsync(deviceFilter: d => !string.IsNullOrEmpty(d.Name))
                .ContinueWith(_ =>
                {
                    if (State != BleState.Scanning)
                    {
                        return;
                    }
                    State = BleState.Idle;
                });
        }


        /// <summary>
        /// 连接到已知的或选中的设备
        /// </summary>
        private async Task Connect()
        {
            State = BleState.Connecting;
            DiscoveredDevices.Clear();

            if (_pairedDevice == null)
            {
                return;
            }

            await _bluetoothLe.Adapter.ConnectToKnownDeviceAsync(_pairedDevice.Id);
        }

        // 在主线程上显示警告/提示框
        private static Task ShowAlert(string title, string message) => MainThread.InvokeOnMainThreadAsync(() =>
        {
            var mainPage = Application.Current?.MainPage;

            return mainPage == null
                ? Task.CompletedTask
                : mainPage.DisplayAlert(title, message, "OK");
        });
    }
}
