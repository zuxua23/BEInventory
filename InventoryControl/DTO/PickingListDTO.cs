namespace InventoryControl.DTO;

public class PickingListDTO
{
    public string DoNumber { get; set; } = null!;
    public List<DODetailCreateDto> Details { get; set; } = new();
}


public class DODetailCreateDto
{
    public string ItemId { get; set; } = null!;
    public int? QtyRequired { get; set; }
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
    public List<DOTagResponseDto>? Tags { get; set; }
}

public class DOTagResponseDto
{
    public string TagId { get; set; }

    public string EpcTag { get; set; }
}