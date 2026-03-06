using System.Runtime.InteropServices;
using System.Text;

namespace InventoryControl.Helpers;

using System;
using System.Runtime.InteropServices;
using System.Text;

public class PrinterHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class DOCINFOA
    {
        public string pDocName;
        public string pOutputFile;
        public string pDataType;
    }

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA")]
    public static extern bool OpenPrinter(string szPrinter, out nint hPrinter, nint pd);

    [DllImport("winspool.Drv", EntryPoint = "ClosePrinter")]
    public static extern bool ClosePrinter(nint hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA")]
    public static extern bool StartDocPrinter(nint hPrinter, int level, DOCINFOA di);

    [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter")]
    public static extern bool EndDocPrinter(nint hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter")]
    public static extern bool StartPagePrinter(nint hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter")]
    public static extern bool EndPagePrinter(nint hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "WritePrinter")]
    public static extern bool WritePrinter(
        nint hPrinter,
        byte[] pBytes,
        int dwCount,
        out int dwWritten);

    public static bool SendStringToPrinter(string printerName, string data)
    {
        nint hPrinter;
        DOCINFOA di = new DOCINFOA();
        di.pDocName = "RFID Tag Print";
        di.pDataType = "RAW";

        if (!OpenPrinter(printerName, out hPrinter, nint.Zero))
            return false;

        StartDocPrinter(hPrinter, 1, di);
        StartPagePrinter(hPrinter);

        byte[] bytes = Encoding.ASCII.GetBytes(data);

        WritePrinter(hPrinter, bytes, bytes.Length, out int written);

        EndPagePrinter(hPrinter);
        EndDocPrinter(hPrinter);
        ClosePrinter(hPrinter);

        return true;
    }
}