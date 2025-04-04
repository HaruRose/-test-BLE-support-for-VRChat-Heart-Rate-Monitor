using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using VRChatHeartRateMonitor;

namespace VRChatHeartRateMonitor
{
    public class BleHandler : IDeviceHandler
    {
        public event Action AdapterError;
        public event Action<ulong, string> DeviceFound;
        public event Action DeviceConnecting;
        public event Action<ulong> DeviceConnected;
        public event Action DeviceDisconnecting;
        public event Action DeviceDisconnected;
        public event Action<string> DeviceError;
        public event Action<ushort> HeartRateUpdated;
        public event Action<ushort> BatteryLevelUpdated;

        private BluetoothLEAdvertisementWatcher bleWatcher;
        private BluetoothLEDevice connectedDevice;
        private GattCharacteristic heartRateCharacteristic;

        private readonly Guid HeartRateServiceUuid = new Guid("0000180D-0000-1000-8000-00805F9B34FB");
        private readonly Guid HeartRateMeasurementCharacteristicUuid = new Guid("00002A37-0000-1000-8000-00805F9B34FB");

        public BleHandler()
        {
            bleWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            bleWatcher.Received += OnAdvertisementReceived;
            bleWatcher.Stopped += OnWatcherStopped;
        }

        public void StartScanning()
        {
            Debug.WriteLine("Starting BLE scan...");
            bleWatcher.Start();
        }

        public void StopScanning()
        {
            Debug.WriteLine("Stopping BLE scan...");
            bleWatcher.Stop();
        }

        public void SubscribeToDevice(ulong bluetoothDeviceAddress)
        {
            // Implement this method based on your requirements.
        }

        public void UnsubscribeFromDevice()
        {
            // Implement this method based on your requirements.
        }

        public ushort GetHeartRate()
        {
            // Implement this method based on your requirements.
            return 0;
        }

        public bool IsListening()
        {
            // Implement this method based on your requirements.
            return false;
        }

        public bool CanConnect()
        {
            // Implement this method based on your requirements.
            return true;
        }

        public string BluetoothAddressToString(ulong bluetoothDeviceAddress)
        {
            return bluetoothDeviceAddress.ToString("X");
        }

        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            foreach (var serviceUuid in args.Advertisement.ServiceUuids)
            {
                if (serviceUuid == HeartRateServiceUuid)
                {
                    Debug.WriteLine($"Found device advertising Heart Rate Service: {args.BluetoothAddress:X}");
                    StopScanning();
                    await ConnectToDeviceAsync(args.BluetoothAddress);
                    break;
                }
            }
        }

        private void OnWatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            Debug.WriteLine("BLE advertisement watcher has stopped.");
        }

        private async Task ConnectToDeviceAsync(ulong bluetoothAddress)
        {
            try
            {
                connectedDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);
                if (connectedDevice == null)
                {
                    Debug.WriteLine("Failed to connect to the BLE device.");
                    return;
                }

                var servicesResult = await connectedDevice.GetGattServicesAsync();
                if (servicesResult.Status != GattCommunicationStatus.Success)
                {
                    Debug.WriteLine("Failed to retrieve GATT services.");
                    return;
                }

                foreach (var service in servicesResult.Services)
                {
                    if (service.Uuid == HeartRateServiceUuid)
                    {
                        var characteristicsResult = await service.GetCharacteristicsAsync();
                        if (characteristicsResult.Status != GattCommunicationStatus.Success)
                        {
                            Debug.WriteLine("Failed to retrieve characteristics for Heart Rate Service.");
                            return;
                        }

                        foreach (var characteristic in characteristicsResult.Characteristics)
                        {
                            if (characteristic.Uuid == HeartRateMeasurementCharacteristicUuid)
                            {
                                heartRateCharacteristic = characteristic;

                                var status = await heartRateCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                if (status == GattCommunicationStatus.Success)
                                {
                                    heartRateCharacteristic.ValueChanged += OnCharacteristicValueChanged;
                                    Debug.WriteLine("Successfully subscribed to heart rate notifications.");
                                }
                                else
                                {
                                    Debug.WriteLine("Failed to subscribe to heart rate characteristic notifications.");
                                }
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception during BLE connection: " + ex.Message);
            }
        }

        private void OnCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            try
            {
                var reader = DataReader.FromBuffer(args.CharacteristicValue);
                byte flags = reader.ReadByte();
                int heartRateValue = 0;
                bool isHeartRateValueLong = (flags & 0x01) != 0;

                if (isHeartRateValueLong)
                {
                    heartRateValue = reader.ReadUInt16();
                }
                else
                {
                    heartRateValue = reader.ReadByte();
                }

                Debug.WriteLine("Received heart rate: " + heartRateValue);
                HeartRateUpdated?.Invoke((ushort)heartRateValue);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error processing heart rate measurement: " + ex.Message);
            }
        }

        public void Disconnect()
        {
            if (heartRateCharacteristic != null)
            {
                heartRateCharacteristic.ValueChanged -= OnCharacteristicValueChanged;
                heartRateCharacteristic = null;
            }

            connectedDevice?.Dispose();
            connectedDevice = null;

            Debug.WriteLine("Disconnected from BLE device.");
        }
    }
}
