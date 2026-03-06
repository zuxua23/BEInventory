using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Transaction")]
public class Transaction
{
    [Key]
    [Column("trs_id")]
    public string TrsId { get; set; }

    [Column("trs_type")]
    public string? TrsType { get; set; }

    [Column("reference_id")]
    public string? ReferenceId { get; set; }

    [Column("rdr_id")]
    public string? ReaderId { get; set; }

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    public Reader? Reader { get; set; }

    public ICollection<Transaction_Detail>? TransactionDetails { get; set; }

}