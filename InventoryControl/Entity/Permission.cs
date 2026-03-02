using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Permission")]
public class Permission
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("per_id")]
    public string PerId { get; set; }

    [Required]
    [Column("per_code")]
    public string Code { get; set; }

    [Required]
    [Column("per_name")]
    public string Name { get; set; }

    [Column("per_group")]
    public int? Group { get; set; }

    [Column("per_desc")]
    public string? Desc { get; set; }


    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("isActive")]
    public bool IsActive { get; set; } = true;
    [Column("isDelete")]
    public bool IsDelete { get; set; } = false;

    public ICollection<Role_Permission> RolePermissions { get; set; }
}