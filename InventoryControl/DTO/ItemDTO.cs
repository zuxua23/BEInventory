namespace InventoryControl.DTO;

public class ItemDto
{
    public string ItemId { get; set; } = null!;
    public string ItemName { get; set; } = null!;
}



public class ItemResponseDto
{
    public string Id { get; set; } = null!;
    public string ItemId { get; set; } = null!;
    public string ItemName { get; set; } = null!;
}