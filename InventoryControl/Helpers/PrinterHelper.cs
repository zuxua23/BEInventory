using System.Runtime.InteropServices;
using System.Text;

public static class RawPrinterHelper
{
    public static bool SendStringToPrinter(string printerName, string data)
    {
        var bytes = SBPLStringToBytes(data);

        IntPtr unmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
        Marshal.Copy(bytes, 0, unmanagedBytes, bytes.Length);

        bool success = SendBytesToPrinter(printerName, unmanagedBytes, bytes.Length);

        Marshal.FreeCoTaskMem(unmanagedBytes);
        return success;
    }
    private static byte[] SBPLStringToBytes(string sbpl)
    {
        var bytes = new List<byte>();

        foreach (char c in sbpl)
        {
            switch (c)
            {
                case '\u0002':
                    bytes.Add(0x02);
                    break;

                case '\u0003':
                    bytes.Add(0x03);
                    break;

                case '\u001B':
                    bytes.Add(0x1B);
                    break;

                default:
                    bytes.Add((byte)c);
                    break;
            }
        }

        return bytes.ToArray();
    }

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA")]
    static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.Drv")]
    static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv")]
    static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFO di);

    [DllImport("winspool.Drv")]
    static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv")]
    static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv")]
    static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv")]
    static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    public static bool SendBytesToPrinter(
        string printerName,
        IntPtr pBytes,
        int dwCount
    )
    {
        bool success = false;

        if (OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero))
        {
            DOCINFO di = new DOCINFO
            {
                pDocName = "SBPL Print",
                pDataType = "RAW"
            };

            if (StartDocPrinter(hPrinter, 1, di))
            {
                if (StartPagePrinter(hPrinter))
                {
                    success = WritePrinter(
                        hPrinter,
                        pBytes,
                        dwCount,
                        out int written
                    );

                    EndPagePrinter(hPrinter);
                }

                EndDocPrinter(hPrinter);
            }

            ClosePrinter(hPrinter);
        }

        return success;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class DOCINFO
    {
        public string pDocName;
        public string pOutputFile;
        public string pDataType;
    }
}