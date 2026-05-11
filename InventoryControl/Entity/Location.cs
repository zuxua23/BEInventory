using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Location")]
public class Location
{
    [Key]
    [Column("id")]
    public string Id { get; set; }

    [Required]
    [Column("loc_id")]
    public string LocId { get; set; }

    [Required]
    [Column("loc_name")]
    public string Name { get; set; }

    [Required]
    [Column("loc_desc")]
    public string Description { get; set; }

    [Required]
    [Column("created_by")]
    public string CreatedBy { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    [Column("isDelete")]
    public bool IsDelete { get; set; } = false;
    [Column("is_system")]
    public bool IsSystem { get; set; } = false;
}
