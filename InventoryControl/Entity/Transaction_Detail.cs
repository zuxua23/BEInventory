using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Transaction_Detail")]
public class Transaction_Detail
{
    [Key]
    [Column("trs_detail_id")]
    [MaxLength(36)]
    public string TrdId { get; set; }

    [Required]
    [Column("trs_id")]
    [MaxLength(36)]
    public string TrsId { get; set; }

    [Required]
    [Column("tag_id")]
    [MaxLength(36)]
    public string TagId { get; set; }

    [Required]
    [Column("item_id")]
    [MaxLength(36)]
    public string ItemId { get; set; }

    public Transaction Transaction { get; set; }
    public Tag Tag { get; set; }
    public Item Item { get; set; }

}