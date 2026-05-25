using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Role_Permission")]
public class Role_Permission
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; } 

    [Required]
    [Column("permission_id")]
    [MaxLength(36)]
    public string PermissionId { get; set; }

    [Required]
    [Column("role_id")]
    [MaxLength(36)]
    public string RoleId { get; set; }

    public Role Role { get; set; }
    public Permission Permission { get; set; }


}