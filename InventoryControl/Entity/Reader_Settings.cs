using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Reader_Settings")]
public class ReaderSettings
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("reader_id")]
    public string ReaderId { get; set; }

    [Column("antenna_no")]
    public int AntennaNo { get; set; }

    [Column("is_enabled")]
    public bool IsEnabled { get; set; }

    [Column("tx_power")]
    public double TxPower { get; set; }

    [Column("sensitivity")]
    public double Sensitivity { get; set; }

    [Column("search_mode")]
    public string SearchMode { get; set; } = "DualTarget";

    [Column("session")]
    public string Session { get; set; } = "S2";

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("isDelete")]
    public bool IsDelete { get; set; } = false;

    public Reader Reader { get; set; }
}