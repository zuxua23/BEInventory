using System.Diagnostics;
using System.Net.Sockets;
using InventoryControl.Utility;

namespace InventoryControl.Helpers;

public static class PW4NX_Helper
{
    public static bool SendToPrinter(
        string ipAddress,
        int port,
        byte[] data
    )
    {
        var stopwatch =
            Stopwatch.StartNew();

        try
        {
            SystemLogger.Info(
                $"Starting printer communication. IP='{ipAddress}', Port='{port}', Bytes='{data.Length}'."
            );

            using var client =
                new TcpClient();

            SystemLogger.Info(
                $"Connecting to printer '{ipAddress}:{port}'."
            );

            client.Connect(
                ipAddress,
                port
            );

            SystemLogger.Info(
                $"Printer connection established successfully. IP='{ipAddress}'."
            );

            using var stream =
                client.GetStream();

            stream.Write(
                data,
                0,
                data.Length
            );

            stream.Flush();

            stopwatch.Stop();

            SystemLogger.Info(
                $"Printer data sent successfully. " +
                $"IP='{ipAddress}', " +
                $"Bytes='{data.Length}', " +
                $"Duration='{stopwatch.ElapsedMilliseconds}ms'."
            );

            return true;
        }
        catch (SocketException ex)
        {
            stopwatch.Stop();

            SystemLogger.Error(
                $"Socket error while communicating with printer '{ipAddress}:{port}'.",
                ex
            );

            return false;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            SystemLogger.Error(
                $"Unexpected printer communication error. IP='{ipAddress}:{port}'.",
                ex
            );

            return false;
        }
    }
}