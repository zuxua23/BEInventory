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