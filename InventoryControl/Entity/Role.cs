using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Role")]
public class Role
{
    [Key]
    [Column("id")]
    public string Id { get; set; }

    [Required]
    [Column("rol_code")]
    public string Code { get; set; }

    [Column("rol_name")]
    public string? Name { get; set; }

    [Column("isActive")]
    public bool IsActive { get; set; } = true;
    [Column("isDelete")]
    public bool IsDelete { get; set; } = false;

    public ICollection<User_Role> UserRoles { get; set; }
    public ICollection<Role_Permission> RolePermissions { get; set; }
}