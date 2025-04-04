using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace VRChatHeartRateMonitor
{
    public class DeviceHandler : IDeviceHandler
    {
        private ushort _heartRate = 0;
        private ushort _batteryLevel = 0;
        private BluetoothLEAdvertisementWatcher _bluetoothLEAdvertisementWatcher;
        private BluetoothLEDevice _device;
        private GattCharacteristic _heartRateCharacteristic;

        private Guid _heartRateServiceUuid = Guid.Parse("0000180D-0000-1000-8000-00805f9b34fb");
        private Guid _heartRateCharacteristicUuid = Guid.Parse("00002A37-0000-1000-8000-00805f9b34fb");

        private GattCharacteristic _batteryLevelCharacteristic;

        private Guid _batteryLevelServiceUuid = Guid.Parse("0000180F-0000-1000-8000-00805f9b34fb");
        private Guid _batteryLevelCharacteristicUuid = Guid.Parse("00002A19-0000-1000-8000-00805f9b34fb");

        private string _errorMessage = null;

        public event Action AdapterError;
        public event Action<ulong, string> DeviceFound;
        public event Action DeviceConnecting;
        public event Action<ulong> DeviceConnected;
        public event Action DeviceDisconnecting;
        public event Action DeviceDisconnected;
        public event Action<string> DeviceError;
        public event Action<ushort> HeartRateUpdated;
        public event Action<ushort> BatteryLevelUpdated;

        public static async Task<bool> CheckCompatibility()
        {
            BluetoothAdapter bluetoothAdapter = await BluetoothAdapter.GetDefaultAsync();

            return (bluetoothAdapter != null && bluetoothAdapter.IsLowEnergySupported);
        }

        public DeviceHandler()
        {
            _bluetoothLEAdvertisementWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            _bluetoothLEAdvertisementWatcher.Received += async (sender, args) => await OnAdvertisementReceivedAsync(args);
        }

        public void StartScanning()
        {
            try
            {
                _bluetoothLEAdvertisementWatcher.Start();
            }
            catch (Exception)
            {
                AdapterError?.Invoke();
            }
        }

        private async Task OnAdvertisementReceivedAsync(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                BluetoothLEDevice bluetoothDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);

                if (bluetoothDevice != null)
                {
                    var result = await bluetoothDevice.GetGattServicesForUuidAsync(_heartRateServiceUuid);

                    if (result.Status == GattCommunicationStatus.Success && result.Services.Count > 0)
                    {
                        DeviceFound?.Invoke(bluetoothDevice.BluetoothAddress, GetDeviceName(bluetoothDevice));
                    }
                }
            }
            catch (Exception) { }
        }

        public void StopScanning()
        {
            _bluetoothLEAdvertisementWatcher.Stop();
        }

        public void StartBLE()
        {
            StartScanning();
        }

        public void SubscribeToDevice(ulong bluetoothDeviceAddress)
        {
            // Implement this method based on your requirements.
        }

        public void UnsubscribeFromDevice()
        {
            // Implement this method based on your requirements.
        }

        public void Disconnect()
        {
            if (_heartRateCharacteristic != null)
            {
                _heartRateCharacteristic.ValueChanged -= HeartRateCharacteristic_ValueChanged;
                _heartRateCharacteristic = null;
            }

            _device?.Dispose();
            _device = null;

            DeviceDisconnected?.Invoke();
        }

        private GattDeviceService GetService(Guid serviceUuid)
        {
            return _device?.GattServices.FirstOrDefault(s => s.Uuid == serviceUuid);
        }

        private GattCharacteristic GetCharacteristic(Guid serviceUuid, Guid characteristicUuid)
        {
            GattDeviceService service = GetService(serviceUuid);

            return service?.GetAllCharacteristics().FirstOrDefault(c => c?.Uuid == characteristicUuid);
        }

        private GattCharacteristic GetHeartRateCharacteristic()
        {
            return GetCharacteristic(_heartRateServiceUuid, _heartRateCharacteristicUuid);
        }

        private GattCharacteristic GetBatteryLevelCharacteristic()
        {
            return GetCharacteristic(_batteryLevelServiceUuid, _batteryLevelCharacteristicUuid);
        }

        private async Task<bool> SubscibeToCharacteristicNotifications(GattCharacteristic characteristic)
        {
            try
            {
                GattCommunicationStatus characteristicCommunicationStatus = await characteristic?.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);

                return (characteristicCommunicationStatus == GattCommunicationStatus.Success);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> SubscibeToHeartRateCharacteristicNotifications()
        {
            return await SubscibeToCharacteristicNotifications(_heartRateCharacteristic);
        }

        private async Task<bool> SubscibeToBatteryLevelCharacteristicNotifications()
        {
            return await SubscibeToCharacteristicNotifications(_batteryLevelCharacteristic);
        }

        private async Task<bool> UnsubscibeToCharacteristicNotifications(GattCharacteristic characteristic)
        {
            try
            {
                GattCommunicationStatus characteristicCommunicationStatus = await characteristic?.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);

                return (characteristicCommunicationStatus == GattCommunicationStatus.Success);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> UnsubscibeToHeartRateCharacteristicNotifications()
        {
            return await UnsubscibeToCharacteristicNotifications(_heartRateCharacteristic);
        }

        private async Task<bool> UnsubscibeToBatteryLevelCharacteristicNotifications()
        {
            return await UnsubscibeToCharacteristicNotifications(_batteryLevelCharacteristic);
        }

        private void HeartRateCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            byte[] data;

            using (var reader = DataReader.FromBuffer(args.CharacteristicValue))
            {
                data = new byte[args.CharacteristicValue.Length];
                reader.ReadBytes(data);
            }

            if (data.Length >= 2)
            {
                byte flags = data[0];
                bool isHeartRateInUInt16 = (flags & 0x01) != 0;

                ushort newHeartRate = (isHeartRateInUInt16 ? BitConverter.ToUInt16(data, 1) : data[1]);

                if (newHeartRate > 254) newHeartRate = 254;

                if (newHeartRate != _heartRate)
                {
                    _heartRate = newHeartRate;
                    HeartRateUpdated?.Invoke(newHeartRate);
                }
            }
        }

        private void SetBatteryLevel(byte[] data)
        {
            ushort newBatteryLevel = data.First();

            if (newBatteryLevel != _batteryLevel)
            {
                _batteryLevel = newBatteryLevel;
                BatteryLevelUpdated?.Invoke(newBatteryLevel);
            }
        }

        private void BatteryLevelCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            SetBatteryLevel(args.CharacteristicValue.ToArray());
        }

        public bool IsListening()
        {
            return (_heartRateCharacteristic != null);
        }

        public bool CanConnect()
        {
            return (_device == null);
        }

        public ushort GetHeartRate()
        {
            return _heartRate;
        }

        public string BluetoothAddressToString(ulong bluetoothAddress)
        {
            return string.Join(":", Enumerable.Range(0, 6).Select(i => $"{bluetoothAddress:X12}".Substring(i * 2, 2)));
        }

        public string GetDeviceAddress(BluetoothLEDevice bluetoothDevice = null)
        {
            if (bluetoothDevice == null)
                bluetoothDevice = _device;

            return BluetoothAddressToString(bluetoothDevice.BluetoothAddress);
        }

        public string GetDeviceName(BluetoothLEDevice bluetoothDevice = null)
        {
            if (bluetoothDevice == null)
                bluetoothDevice = _device;

            return bluetoothDevice.Name + " (" + GetDeviceAddress(bluetoothDevice) + ")";
        }
    }
}
