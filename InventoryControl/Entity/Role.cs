using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Role")]
public class Role
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; }

    [Required]
    [Column("rol_code")]
    [MaxLength(30)]
    public string Code { get; set; }

    [Column("rol_name")]
    [MaxLength(50)]
    public string? Name { get; set; }

    [Column("isActive")]
    public bool IsActive { get; set; } = true;
    [Column("isDelete")]
    public bool IsDelete { get; set; } = false;
        
    public ICollection<User_Role> UserRoles { get; set; }
    public ICollection<Role_Permission> RolePermissions { get; set; }
}