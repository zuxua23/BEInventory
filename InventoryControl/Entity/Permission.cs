using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Permission")]
public class Permission
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("module_id")]
    [MaxLength(36)]
    public string? ModuleId { get; set; }

    [Column("operation")]
    [MaxLength(50)]
    public string? Operation { get; set; }

    [Required]
    [Column("per_code")]
    [MaxLength(50)]
    public string Code { get; set; }

    [Required]
    [Column("per_name")]
    [MaxLength(50)]
    public string Name { get; set; }

    [Column("created_by")]
    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("isActive")]
    public bool IsActive { get; set; } = true;
    [Column("isDelete")]
    public bool IsDelete { get; set; } = false;

    public ICollection<Role_Permission> RolePermissions { get; set; }
    public Module Module { get; set; }
}