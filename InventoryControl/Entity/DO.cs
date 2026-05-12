using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_DO")]
public class DO
{
    [Key]
    [Column("do_id")]
    public string DoId { get; set; }

    [Column("do_number")]
    public string? DoNumber { get; set; }

    [Column("scanner_type")]
    public string? ScannerType { get; set; }

    [Column("status")]
    public string? Status { get; set; }  //DRAFT → PREPARATION → COMPLETED

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("isDelete")]
    public bool IsDelete { get; set; } = false;

    public ICollection<DODetail>? Details { get; set; }
}