/// <summary>
///  To resolve this error, ensure that BleHandler implements or inherits from DeviceHandler.
/// </summary>
using System;

namespace VRChatHeartRateMonitor
{
    public interface IDeviceHandler
    {
        event Action AdapterError;
        event Action<ulong, string> DeviceFound;
        event Action DeviceConnecting;
        event Action<ulong> DeviceConnected;
        event Action DeviceDisconnecting;
        event Action DeviceDisconnected;
        event Action<string> DeviceError;
        event Action<ushort> HeartRateUpdated;
        event Action<ushort> BatteryLevelUpdated;

        void StartScanning();
        void StopScanning();
        void Disconnect();
        void SubscribeToDevice(ulong bluetoothDeviceAddress);
        void UnsubscribeFromDevice();
        ushort GetHeartRate();
        bool IsListening();
        bool CanConnect();
        string BluetoothAddressToString(ulong bluetoothDeviceAddress);
    }
}
