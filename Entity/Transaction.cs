using InventoryControl.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Transaction")]
public class Transaction
{
    [Key]
    [Column("trs_id")]
    [MaxLength(36)]
    public string TrsId { get; set; }

    [Column("trs_type")]
    [MaxLength(30)]
    public TransactionType TrsType { get; set; }

    [Column("reference_id")]
    [MaxLength(36)]
    public string? ReferenceId { get; set; }

    [Column("reader_id")]
    [MaxLength(36)]
    public string? ReaderId { get; set; }

    [Column("created_by")]
    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    public Reader? Reader { get; set; }

    public ICollection<Transaction_Detail>? TransactionDetails { get; set; }

}