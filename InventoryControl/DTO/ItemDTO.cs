namespace InventoryControl.DTO;

public class ItemDto
{
    public string ItemName { get; set; } = null!;
    public string? ItemDesc { get; set; }
}

public class ItemResponseDto
{
    public string Id { get; set; } = null!;
    public string ItemId { get; set; } = null!;
    public string ItemName { get; set; } = null!;
    public string? ItemDesc { get; set; }
}
public class ItemListDto
{
    public string ItemId { get; set; }
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public int Required { get; set; }
    public int Reserved { get; set; }
    public int Scanned { get; set; }
}

public class ProgressDto
{
    public int Total { get; set; }
    public int Scanned { get; set; }
}

public class TagDto
{
    public string TagId { get; set; }
    public string ItemId { get; set; }
}