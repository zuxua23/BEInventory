namespace InventoryControl.DTO;

public class ActivityLogDto
{
    public string Time { get; set; }
    public string Action { get; set; }
    public string Entity { get; set; }
    public string User { get; set; }
}
