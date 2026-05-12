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
    public string Id { get; set; }

    [Required]
    [Column("itm_id")]
    public string ItmId { get; set; }
    [Required]
    [Column("itm_name")]
    public string Name { get; set; }

    [Required]
    [Column("itm_desc")]
    public string? Description { get; set; }

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
    public bool IsDelete { get; set; }

    public ICollection<Transaction_Detail>? TransactionDetails { get; set; }
    //public string ItemName { get; internal set; }
}
