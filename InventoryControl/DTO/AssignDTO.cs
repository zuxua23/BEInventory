namespace InventoryControl.DTO;

public class AssignPermissionDto
{
    public List<string> PermissionIds { get; set; } = new();
}

public class AssignRoleDto
{
    public List<string> RoleIds { get; set; } = new();
}


public class UpdateConnectionDto
{
    public string Server { get; set; }
    public string Database { get; set; }
    public string UserId { get; set; }
    public string Password { get; set; }
}