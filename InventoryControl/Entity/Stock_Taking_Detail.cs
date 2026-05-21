using InventoryControl.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Stock_Taking_Detail")]
public class StockTakingDetail
{
    [Key]
    [Column("st_detail_id")]
    public string StdId { get; set; }

    [Column("stt_id")]
    public string SttId { get; set; }

    [Column("tag_id")]
    public string TagId { get; set; }

    [Column("item_id")]
    public string? ItemId { get; set; }

    [Column("remark")]
    public string? Remark { get; set; }

    [Column("action")]
    public TakingAction Action { get; set; }

    public StockTaking? StockTaking { get; set; }

    public Tag? Tag { get; set; }
    public Item? Item { get; set; }
}