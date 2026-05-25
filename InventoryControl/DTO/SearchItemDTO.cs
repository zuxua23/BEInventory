namespace InventoryControl.DTO;

public class SearchItemListDto
{
    public string TagId { get; set; }
    public string EpcTag { get; set; }
    public string ItemName { get; set; }
    public string Location { get; set; }
}

public class TagDetailDto
{
    public string TagId { get; set; }
    public string EpcTag { get; set; }
    public string ItemName { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
}