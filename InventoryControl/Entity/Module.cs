using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Module")]
public class Module
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; }

    [Column("mod_key")]
    [MaxLength(30)]
    public string ModuleKey { get; set; }

    [Column("mod_name")]
    [MaxLength(50)]
    public string ModuleName { get; set; }

    [Column("isActive")]
    public bool IsActive { get; set; } = true;

    public ICollection<Permission> Permissions { get; set; }
}
