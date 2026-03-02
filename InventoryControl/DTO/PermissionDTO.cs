namespace InventoryControl.DTO;

public class PermissionDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public class PermissionResponseDto
{
    public string Id { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}