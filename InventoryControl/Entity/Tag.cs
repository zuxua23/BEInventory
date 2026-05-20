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
    public string Id { get; set; }

    [Required]
    [Column("tag_id")]
    public string TagId { get; set; }

    [Required]
    [Column("itm_id")]
    public string ItemId { get; set; }

    [Column("tag_epc")]
    public string EpcTag { get; set; }

    [Required]
    [Column("status")]
    public TagStatus Status { get; set; } //PRINTED / IN_STOCK/ RESERVED /OUT /STANBY

    [Required]
    [Column("loc_id")]
    public string LocationId { get; set; }

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

    public Location Location { get; set; }
    public Item Item { get; set; }

    public ICollection<Transaction_Detail>? TransactionDetails { get; set; }

}