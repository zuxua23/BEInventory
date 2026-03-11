namespace InventoryControl.DTO;

public class DODTO
{
    public string DoNumber { get; set; } = null!;
    public string ScannerType { get; set; } = null!;
    public List<DODetailCreateDto> Details { get; set; } = new();
}
public class DOUpdateDTO
{
    public string DoId { get; set; } = null!;
    public string DoNumber { get; set; } = null!;
    public string ScannerType { get; set; } = null!;
    public List<DODetailCreateDto> Details { get; set; } = new();
}

public class DODetailCreateDto
{
    public string ItemId { get; set; } = null!;
    public int QtyRequired { get; set; }
}

public class DOResponseDto
{
    public string DoId { get; set; } = null!;
    public string? DoNumber { get; set; }
    public string? ScannerType { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }

    public List<DODetailResponseDto> Details { get; set; } = new();
}
public class DODetailResponseDto
{
    public string DoDetailId { get; set; } = null!;
    public string ItemId { get; set; } = null!;
    public string? ItemName { get; set; }
    public int? QtyRequired { get; set; }
}
public class DOStatusUpdateDto
{
    public string Status { get; set; } = null!;
}