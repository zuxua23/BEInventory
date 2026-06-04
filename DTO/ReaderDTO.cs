using InventoryControl.Models;

namespace InventoryControl.DTO;

public class ReaderDto
{
    public string RdrId { get; set; } = null!;
    public string LocId { get; set; } = null!;
    public string RdrName { get; set; } = null!;
    public string IpAddress { get; set; } = null!;
}

public class ReaderResponseDto
{
    public string Id { get; set; } = null!;
    public string RdrId { get; set; } = null!;
    public string RdrName { get; set; } = null!;
    public string LocId { get; set; } = null!;
    public string? LocationName { get; set; }
    public string IpAddress { get; set; }
}

public class ReaderScanDto
{
    public string DoId { get; set; }
    public string ReaderId { get; set; }
    public string Epc { get; set; }
}


public class ReaderSettingDto
{
    public string ReaderId { get; set; }
    public ReaderSearchMode SearchMode { get; set; } = ReaderSearchMode.DualTarget;
    public ReaderSession Session { get; set; } = ReaderSession.S2;
    public int DuplicateScanInterval { get; set; } = 2;
    public List<ReaderSettingItemDto> Antennas { get; set; } = new();
}

public class ReaderSettingItemDto
{
    public int AntennaNo { get; set; }
    public bool IsEnabled { get; set; }
    public double TxPower { get; set; }
    public double Sensitivity { get; set; }
}

public class RfidAdvancedSettingDto
{
    public int ConnectionRetry { get; set; }
    public int RetryDelayMs { get; set; }
    public int CacheTTLSeconds { get; set; }
}