using InventoryControl.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Stock_Taking")]
public class StockTaking
{
    [Key]
    [Column("stt_id")]
    [MaxLength(36)]
    public string SttId { get; set; }

    [Column("remark")]
    [MaxLength(255)]
    public string? Remark { get; set; }


    [Column("status")]
    [MaxLength(30)]
    public TakingStatus Status { get; set; }

    [Column("created_by")]
    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    public ICollection<StockTakingDetail>? Details { get; set; }

}