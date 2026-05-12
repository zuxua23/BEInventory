using System.ComponentModel.DataAnnotations;

namespace InventoryControl.DTO;

public class UserDto
{
    public string Fullname { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public List<string> RoleIds { get; set; }
}
public class UpdateUserDto
{
    public string UserId { get; set; } = null!;      
    public string Fullname { get; set; } = null!;
    public string Username { get; set; } = null!;
}
public class UserResponseDto
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string Fullname { get; set; }
    public string Username { get; set; }
    public List<string> Roles { get; set; }
}
public class UpdateUserRoleDto
{
    public string UserId { get; set; }
    public List<string> Roles { get; set; } 
}
public class UpdatePasswordDto
{
    public string Password { get; set; }
}