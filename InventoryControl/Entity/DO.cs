using InventoryControl.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_DO")]
public class DO
{
    [Key]
    [Column("do_id")]
    [MaxLength(36)]
    public string DoId { get; set; }

    [Column("do_number")]
    [MaxLength(50)]
    public string? DoNumber { get; set; }

    [Column("scanner_type")]
    [MaxLength(30)]
    public string? ScannerType { get; set; }

    [Column("status")]
    [MaxLength(30)]
    public DoStatus Status { get; set; }  //DRAFT → PREPARATION → COMPLETED

    [Column("created_by")]
    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_by")]
    [MaxLength(50)]
    public string? UpdatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("isDelete")]
    public bool IsDelete { get; set; } = false;

    public ICollection<DODetail>? Details { get; set; }
}