using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.Net.Sockets;
using System.Text;

namespace InventoryControl.Helpers;

public static class PW4NX_Helper
{
    public static bool SendToPrinter(string ipAddress, int port, byte[] data)
    {
        try
        {
            Console.WriteLine("=== START PRINT DEBUG ===");
            Console.WriteLine($"IP: {ipAddress}");
            Console.WriteLine($"PORT: {port}");
            Console.WriteLine($"BYTES LENGTH: {data.Length}");

            Console.WriteLine("DATA (ASCII):");
            Console.WriteLine(Encoding.ASCII.GetString(data));

            using (TcpClient client = new TcpClient())
            {
                Console.WriteLine("Connecting to printer...");
                client.Connect(ipAddress, port);
                Console.WriteLine("Connected ✔");

                using (NetworkStream stream = client.GetStream())
                {
                    Console.WriteLine("Sending data...");
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                    Console.WriteLine("Data sent ✔");
                }
            }

            Console.WriteLine("=== END PRINT DEBUG ===");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Printer Error: " + ex.Message);
            return false;
        }
    }
}