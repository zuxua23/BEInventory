using InventoryControl.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_History_Print")]
public class HistoryPrint
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; }

    [Required]
    [Column("item_id")]
    [MaxLength(36)]
    public string ItemId { get; set; }

    [Required]
    [Column("tag_id")]
    [MaxLength(36)]
    public string TagId { get; set; }

    [Column("trs_type")]
    [MaxLength(30)]
    public HistoryType Type { get; set; }

    [Column("ref_no")]
    [MaxLength(30)]
    public string Reference { get; set; }

    [Column("created_by")]
    [MaxLength(50)]
    public string CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("action")]
    [MaxLength(50)]
    public string Action { get; set; }

    public Item Item { get; set; }
    public Tag Tag { get; set; }
}
