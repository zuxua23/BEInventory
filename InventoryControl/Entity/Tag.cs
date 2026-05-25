using InventoryControl.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Tag")]
[Index(nameof(TagId), IsUnique = true)]

public class Tag
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; }

    [Required]
    [Column("tag_id")]
    [MaxLength(10)]
    public string TagId { get; set; }

    [Required]
    [Column("item_id")]
    [MaxLength(36)]
    public string ItemId { get; set; }

    [Column("tag_epc")]
    [MaxLength(30)]
    public string EpcTag { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(30)]
    public TagStatus Status { get; set; } //PRINTED / IN_STOCK/ RESERVED /OUT /STANBY

    [Required]
    [Column("location_id")]
    [MaxLength(36)]
    public string LocationId { get; set; }

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

    public Location Location { get; set; }
    public Item Item { get; set; }

    public ICollection<Transaction_Detail>? TransactionDetails { get; set; }

}