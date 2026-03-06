using Impinj.OctaneSdk;

namespace InventoryControl.Helpers;

public class ImpinjHelper
{
    private ImpinjReader _reader;

    public void Connect(string ip)
    {
        _reader = new ImpinjReader();
        _reader.Connect(ip);
    }

    public void StartReading(Action<string> onTagRead)
    {
        var settings = _reader.QueryDefaultSettings();

        settings.Report.IncludePeakRssi = true;
        settings.Report.IncludeAntennaPortNumber = true;

        _reader.ApplySettings(settings);

        _reader.TagsReported += (reader, report) =>
        {
            foreach (Tag tag in report)
            {
                var epc = tag.Epc.ToString();
                onTagRead(epc);
            }
        };

        _reader.Start();
    }

    public void Stop()
    {
        if (_reader != null && _reader.IsConnected)
        {
            _reader.Stop();
            _reader.Disconnect();
        }
    }
}