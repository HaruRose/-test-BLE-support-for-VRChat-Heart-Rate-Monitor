/// This just decides automatically between BLE Bluetooth Low energy (some wristbands, smartwatches) using GATT and other BE connections.
using System;
using BleHandler;

namespace VRChatHeartRateMonitor
{
    /// <summary>
    /// Manages the heart rate connection by choosing either BLE (via BleHandler)
    /// or the classic Bluetooth connection (via DirectBluetoothHandler).
    /// </summary>
    public class HeartRateConnectionManager
    {
        // Hardcoded flag to force BLE usage. Set to true for BLE, or false to use classic.
        private bool useBle = true;

        // Instances of connection handlers.
        private BleHandler bleHandler;
        private DirectBluetoothHandler directHandler; // Assume this exists in your project.

        /// <summary>
        /// Initializes the connection based on the hardcoded connection mode.
        /// </summary>
        public void Initialize()
        {
            if (useBle)
            {
                // Instantiate and use the BLE handler.
                bleHandler = new BleHandler();
                bleHandler.HeartRateUpdated += ProcessHeartRate;
                bleHandler.StartScanning();
                Console.WriteLine("Initialized using BLE connection for heart rate monitoring.");
            }
            else
            {
                // Use existing classic Bluetooth connection logic.
                directHandler = new DirectBluetoothHandler();
                directHandler.HeartRateUpdated += ProcessHeartRate;
                directHandler.StartConnecting();
                Console.WriteLine("Initialized using Classic Bluetooth connection for heart rate monitoring.");
            }
        }

        /// <summary>
        /// Callback method to process the new heart rate measurement.
        /// </summary>
        /// <param name="heartRate">The heart rate measurement received.</param>
        private void ProcessHeartRate(int heartRate)
        {
            Console.WriteLine("New heart rate: " + heartRate);
            // Insert any additional processing or UI update logic here.
        }

        /// <summary>
        /// Disconnects from the current connection (BLE or classic).
        /// </summary>
        public void Disconnect()
        {
            if (useBle)
                bleHandler?.Disconnect();
            else
                directHandler?.Disconnect();
        }
    }
}
