using InventoryControl.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Stock_Taking_Detail")]
public class StockTakingDetail
{
    [Key]
    [Column("st_detail_id")]
    [MaxLength(36)]
    public string StdId { get; set; }

    [Column("stt_id")]
    [MaxLength(36)]
    public string SttId { get; set; }

    [Column("tag_id")]
    [MaxLength(36)]
    public string TagId { get; set; }

    [Column("item_id")]
    [MaxLength(36)]
    public string? ItemId { get; set; }

    [Column("remark")]
    [MaxLength(255)]
    public string? Remark { get; set; }

    [Column("action")]
    [MaxLength(30)]
    public TakingAction Action { get; set; }

    public StockTaking? StockTaking { get; set; }

    public Tag? Tag { get; set; }
    public Item? Item { get; set; }
}