namespace InventoryControl.DTO;

public class StockOutDto
{
    public string DoId { get; set; } = null!;
    public string ReaderId { get; set; } = null!;
}

public class StockOutResponseDto
{
    public string DoId { get; set; } = null!;
    public string ReaderId { get; set; } = null!;
    public string Epc { get; set; } = null!;
}

public class StartScanDto
{
    public string ReaderId { get; set; } = null!;
    public string DoId { get; set; } = null!;
    public string IpAddress { get; set; } = null!;
}
public class ImpinjDto
{
    public string ReaderId { get; set; }
    public string DoId { get; set; }
    public List<TagData> Tags { get; set; }
}

public class TagData
{
    public string Epc { get; set; }
    public int Antenna { get; set; }
    public int Rssi { get; set; }
}

