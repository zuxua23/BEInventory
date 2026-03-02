namespace InventoryControl.DTO;

public class AssignPermissionDto
{
    public List<string> PermissionIds { get; set; } = new();
}

public class AssignRoleDto
{
    public List<string> RoleIds { get; set; } = new();
}