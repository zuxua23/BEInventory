using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Location")]
public class Location
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; }

    [Required]
    [Column("loc_id")]
    [MaxLength(50)]
    public string LocId { get; set; }

    [Required]
    [Column("loc_name")]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    [Column("loc_desc")]
    [MaxLength(255)]
    public string Description { get; set; }

    [Required]
    [Column("created_by")]
    [MaxLength(50)]
    public string CreatedBy { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_by")]
    [MaxLength(50)]
    public string? UpdatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    [Column("isDelete")]
    public bool IsDelete { get; set; } = false;
    [Column("isSystem")]
    public bool IsSystem { get; set; } = false;
}
