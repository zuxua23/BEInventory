using System.Runtime.InteropServices;
using System.Text;

public static class RawPrinterHelper
{
    private static readonly SemaphoreSlim _printerLock = new(1, 1);

  public static async Task SendStringToPrinterAsync( string printerName,string data)
    {
        await _printerLock.WaitAsync();

        try
        {
            var bytes = SBPLStringToBytes(data);

            IntPtr unmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);

            try
            {
                Marshal.Copy(
                    bytes,
                    0,
                    unmanagedBytes,
                    bytes.Length
                );

                 SendBytesToPrinter(
                    printerName,
                    unmanagedBytes,
                    bytes.Length
                );
            }
            finally
            {
                Marshal.FreeCoTaskMem(
                    unmanagedBytes
                );
            }
        }
        finally
        {
            _printerLock.Release();
        }
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

                    bytes.AddRange(
                        Encoding.ASCII
                            .GetBytes(
                                c.ToString()
                            )
                    );

                    break;
            }
        }

        return bytes.ToArray();
    }


    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true)]

    private static extern bool OpenPrinter(string pPrinterName,out IntPtr phPrinter,IntPtr pDefault);

    [DllImport( "winspool.drv",SetLastError = true )]
    private static extern bool   ClosePrinter( IntPtr hPrinter );

    [DllImport( "winspool.drv",  SetLastError = true)]
    private static extern bool StartDocPrinter(  IntPtr hPrinter, int level,  [In] DOCINFO di );

    [DllImport( "winspool.drv",  SetLastError = true )]
    private static extern bool   EndDocPrinter( IntPtr hPrinter  );

    [DllImport(  "winspool.drv",  SetLastError = true  )]
    private static extern bool StartPagePrinter(   IntPtr hPrinter  );

    [DllImport( "winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter );

    [DllImport(   "winspool.drv",  SetLastError = true   )]
    private static extern bool  WritePrinter(  IntPtr hPrinter, IntPtr pBytes, int dwCount,  out int dwWritten  );



    private static bool  SendBytesToPrinter( string printerName, IntPtr pBytes,   int dwCount  )
    {
        if (
            !OpenPrinter(  printerName, out IntPtr hPrinter, IntPtr.Zero )
        )
        {
            var error = Marshal.GetLastWin32Error();

            throw new Exception(
                $"Failed to open printer. Win32 Error: {error}"
            );
        }

        try
        {
            DOCINFO di = new()
            {
                pDocName = "SBPL Print",
                pDataType = "RAW"
            };

            if (
                !StartDocPrinter(  hPrinter, 1, di )
            )
            {
                var error = Marshal.GetLastWin32Error();

                throw new Exception(
                    $"Failed to start printer document. Win32 Error: {error}"
                );
            }

            try
            {
                if (  !StartPagePrinter( hPrinter )  )
                {
                    var error = Marshal.GetLastWin32Error();

                    throw new Exception(
                        $"Failed to start printer page. Win32 Error: {error}"
                    );
                }

                try
                {
                    bool success = WritePrinter(
                            hPrinter,
                            pBytes,
                            dwCount,
                            out int written
                        );

                    if (!success)
                    {
                        var error =   Marshal.GetLastWin32Error();

                        throw new Exception(
                            $"Failed to write printer data. Win32 Error: {error}"
                        );
                    }

                    if (written != dwCount)
                    {
                        throw new Exception(
                            $"Incomplete printer write. Expected {dwCount} bytes but only {written} bytes written."
                        );
                    }

                    return true;
                }
                finally
                {
                    EndPagePrinter(
                        hPrinter
                    );
                }
            }
            finally
            {
                EndDocPrinter(
                    hPrinter
                );
            }
        }
        finally
        {
            ClosePrinter(hPrinter);
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public class DOCINFO
    {
        public string pDocName;
        public string pOutputFile;
        public string pDataType;
    }
}