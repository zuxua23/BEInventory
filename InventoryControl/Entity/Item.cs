using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Item")]
[Index(nameof(ItmId), IsUnique = true)]

public class Item
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; }

    [Required]
    [Column("itm_id")]
    [MaxLength(10)]
    public string ItmId { get; set; }
    [Required]
    [Column("itm_name")]
    [MaxLength(100)]
    public string Name { get; set; }

    [Column("itm_desc")]
    [MaxLength(255)]    
    public string? Description { get; set; }

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
    public bool IsDelete { get; set; }

    public ICollection<Transaction_Detail>? TransactionDetails { get; set; }

}
