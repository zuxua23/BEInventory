using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Role_Permission")]
public class Role_Permission
{
    [Key]
    [Column("id")]
    public string Id { get; set; } 
    [Required]
    [Column("permission_id")]
    public string PermissionId { get; set; }
    [Required]
    [Column("role_id")]
    public string RoleId { get; set; }

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("is_override")]
    public int? Override { get; set; }

    public Role Role { get; set; }
    public Permission Permission { get; set; }


}