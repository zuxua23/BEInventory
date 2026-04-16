namespace InventoryControl.DTO;

public class TagCreateDto
{
    public string ItemId { get; set; } = null!;
    public string EpcTag { get; set; } = null!;
    public string CurrentLocId { get; set; } = null!;
}

public class TagResponseDto
{
    public string TagId { get; set; } = null!;
    public string? ItemName { get; set; }
    public string? Status { get; set; }
    public string? Location { get; set; }
}

public class TagRegistrationDto
{
    public List<string> TagIds { get; set; } = new();
}
public class PrintTagDto
{
    public string ItemId { get; set; } = null!;
    public int Qty { get; set; }
}

public class PrintHistoryResponseDto
{
    public string TagId { get; set; } = null!;
    public string ItemId { get; set; } = null!;
    public string ItemName { get; set; } = null!;
    public string LocId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string BatchNo { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class PrintDto
{
    public string ItemName { get; set; }
    public int Qty { get; set; }
}