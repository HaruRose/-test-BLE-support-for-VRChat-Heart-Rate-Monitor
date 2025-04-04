/// This just decides automatically between BLE Bluetooth Low energy (some wristbands, smartwatches) using GATT and other BE connections.
using System;
using BleHandler;
using VRChatHeartRateMonitor;

public class HeartRateConnectionManager
{
    private bool useBle = true;
    private BleHandler bleHandler;
    private DirectBluetoothHandler directHandler;

    public void Initialize()
    {
        try
        {
            if (useBle)
            {
                bleHandler = new BleHandler();
                bleHandler.HeartRateUpdated += ProcessHeartRate;
                bleHandler.StartScanning();
                Console.WriteLine("Initialized using BLE connection for heart rate monitoring.");
            }
            else
            {
                directHandler = new DirectBluetoothHandler();
                directHandler.HeartRateUpdated += ProcessHeartRate;
                directHandler.StartConnecting();
                Console.WriteLine("Initialized using Classic Bluetooth connection for heart rate monitoring.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to initialize connection: " + ex.Message);
        }
    }

    private void ProcessHeartRate(int heartRate)
    {
        Console.WriteLine("New heart rate: " + heartRate);
        // Insert any additional processing or UI update logic here.
    }

    public void Disconnect()
    {
        try
        {
            if (useBle)
                bleHandler?.Disconnect();
            else
                directHandler?.Disconnect();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to disconnect: " + ex.Message);
        }
    }
}
