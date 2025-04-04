using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace VRChatHeartRateMonitor
{
    /// <summary>
    /// BleHandler is responsible for scanning for BLE devices advertising the Heart Rate Service (UUID 0x180D),
    /// connecting to one, and then subscribing to the Heart Rate Measurement Characteristic (UUID 0x2A37).
    /// When data is received, it raises a HeartRateUpdated event with the new value.
    /// </summary>
    public class BleHandler
    {
        // Event fired whenever a new heart rate measurement is available.
        public event Action<int> HeartRateUpdated;

        // BLE watcher instance.
        private BluetoothLEAdvertisementWatcher bleWatcher;

        // The connected BLE device.
        private BluetoothLEDevice connectedDevice;

        // The GATT characteristic for heart rate measurement.
        private GattCharacteristic heartRateCharacteristic;

        // Standard UUIDs for BLE Heart Rate Service and Measurement Characteristic.
        private readonly Guid HeartRateServiceUuid = new Guid("0000180D-0000-1000-8000-00805F9B34FB");
        private readonly Guid HeartRateMeasurementCharacteristicUuid = new Guid("00002A37-0000-1000-8000-00805F9B34FB");

        public BleHandler()
        {
            // Initialize the BLE advertisement watcher.
            bleWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            bleWatcher.Received += OnAdvertisementReceived;
            bleWatcher.Stopped += OnWatcherStopped;
        }

        /// <summary>
        /// Begins scanning for BLE devices.
        /// </summary>
        public void StartScanning()
        {
            Debug.WriteLine("Starting BLE scan...");
            bleWatcher.Start();
        }

        /// <summary>
        /// Stops the BLE device scan.
        /// </summary>
        public void StopScanning()
        {
            Debug.WriteLine("Stopping BLE scan...");
            bleWatcher.Stop();
        }

        /// <summary>
        /// Handles incoming BLE advertisements. Looks for devices advertising the Heart Rate Service.
        /// </summary>
        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            // Loop through the advertised service UUIDs.
            foreach (var serviceUuid in args.Advertisement.ServiceUuids)
            {
                if (serviceUuid == HeartRateServiceUuid)
                {
                    Debug.WriteLine($"Found device advertising Heart Rate Service: {args.BluetoothAddress:X}");
                    
                    // Stop scanning as we found a candidate.
                    StopScanning();

                    // Attempt to connect to the device.
                    await ConnectToDeviceAsync(args.BluetoothAddress);
                    break;
                }
            }
        }

        /// <summary>
        /// Handles the BLE watcher stopped event.
        /// </summary>
        private void OnWatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            Debug.WriteLine("BLE advertisement watcher has stopped.");
        }

        /// <summary>
        /// Connects to the device with the specified Bluetooth address,
        /// discovers the Heart Rate Service and its Measurement Characteristic,
        /// then subscribes to notifications.
        /// </summary>
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

                // Get all GATT services.
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
                        // Get all characteristics for the Heart Rate Service.
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

                                // Subscribe to notifications.
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
                        // After processing Heart Rate Service, no need to look in other services.
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception during BLE connection: " + ex.Message);
            }
        }

        /// <summary>
        /// Callback invoked whenever the Heart Rate Measurement characteristic value changes.
        /// Decodes the received data as per BLE specifications.
        /// </summary>
        private void OnCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            try
            {
                var reader = DataReader.FromBuffer(args.CharacteristicValue);

                // The first byte is the flags.
                byte flags = reader.ReadByte();
                int heartRateValue = 0;
                bool isHeartRateValueLong = (flags & 0x01) != 0;

                // Check flag for value format: 8-bit or 16-bit.
                if (isHeartRateValueLong)
                {
                    heartRateValue = reader.ReadUInt16();
                }
                else
                {
                    heartRateValue = reader.ReadByte();
                }

                Debug.WriteLine("Received heart rate: " + heartRateValue);

                // Notify subscribers with the new heart rate value.
                HeartRateUpdated?.Invoke(heartRateValue);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error processing heart rate measurement: " + ex.Message);
            }
        }

        /// <summary>
        /// Disconnects from the BLE device and cleans up any event handlers.
        /// </summary>
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
