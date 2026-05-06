namespace InventoryControl.DTO;

public class LoginDTO
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class LoginResultDto
{
    public string UserId { get; set; }

    public string Username { get; set; }

    public List<string> Roles { get; set; }

    public List<string> Permissions { get; set; }
}