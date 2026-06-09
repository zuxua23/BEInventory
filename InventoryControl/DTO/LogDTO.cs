namespace InventoryControl.DTO;

public class ActivityLogDto
{
    public string Time { get; set; }
    public string Action { get; set; }
    public string Entity { get; set; }
    public string EntityId { get; set; }
    public string User { get; set; }
    public string Description { get; set; }
}
