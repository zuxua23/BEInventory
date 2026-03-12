using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.Text;

namespace InventoryControl.Helpers;

public class BluetoothPrinterHelper
{
    public static bool SendRawBytes(string macAddress, byte[] data)
    {
        try
        {
            // normalize MAC
            macAddress = macAddress.Replace(":", "")
                                   .Replace("-", "")
                                   .Trim()
                                   .ToUpper();

            var address = BluetoothAddress.Parse(macAddress);

            using var client = new BluetoothClient();
            var ep = new BluetoothEndPoint(address, BluetoothService.SerialPort);

            client.Connect(ep);

            using var stream = client.GetStream();

            stream.Write(data, 0, data.Length);
            stream.Flush();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Bluetooth Print Error: " + ex.Message);
            return false;
        }
    }
}