namespace InventoryControl.DTO;

public class PermissionDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}
public class PermissionUpdateDto
{
    public string PermissionId { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public class PermissionResponseDto
{
    public string Id { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}
public class RoleRequestDto
{
    public string RoleCode { get; set; }
    public string RoleName { get; set; }
    public Dictionary<string, List<string>> Permissions { get; set; }
}
public class RoleResponseDto
{
    public string Id { get; set; }
    public string RoleCode { get; set; }
    public string RoleName { get; set; }
    public Dictionary<string, List<string>> Permissions { get; set; }
}
public class PermissionSeed
{
    public string Module { get; set; }
    public string Operation { get; set; }
    public string Name { get; set; }
}