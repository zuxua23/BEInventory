namespace InventoryControl.DTO;

public class LocationDTO
{
    public string LocId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}
public class LocationResponseDTO
{
    public string Id { get; set; }
    public string LocId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}