using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_DO_Detail")]
public class DODetail
{
    [Key]
    [Column("do_detail_id")]
    [MaxLength(36)]
    public string DoDetailId { get; set; }

    [Column("do_id")]
    [MaxLength(36)]
    public string DoId { get; set; }

    [Column("itm_id")]
    [MaxLength(36)]
    public string ItemId { get; set; }

    [Column("qty_required")]
    public int? QtyRequired { get; set; }

    public DO? DO { get; set; }

    public Item? Item { get; set; }
    public ICollection<DODetailTag>? DODetailTags { get; set; }
}