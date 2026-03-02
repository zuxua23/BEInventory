namespace InventoryControl.DTO;

public class RoleDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public class RoleResponseDto
{
    public string Id { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}