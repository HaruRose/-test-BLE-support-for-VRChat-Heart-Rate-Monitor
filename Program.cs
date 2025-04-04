using System;
using VRChatHeartRateMonitor; // Adjust namespace if necessary

class Program
{
    static void Main(string[] args)
    {
        var connectionManager = new HeartRateConnectionManager();
        connectionManager.Initialize();

        Console.WriteLine("Heart rate monitoring started. Press any key to exit...");
        Console.ReadKey();

        connectionManager.Disconnect();
    }
}
