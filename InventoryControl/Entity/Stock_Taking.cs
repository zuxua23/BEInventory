using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Stock_Taking")]
public class StockTaking
{
    [Key]
    [Column("stt_id")]
    public string SttId { get; set; }

    [Column("remark")]
    public string? Remark { get; set; }

    [Column("loc_id")]
    public string LocationId { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    public ICollection<StockTakingDetail>? Details { get; set; }

    public Location LocationTaking { get; set; }



}