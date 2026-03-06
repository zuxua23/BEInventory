using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_History_Print")]
public class HistoryPrint
{
    [Key]
    [Column("id")]
    public string Id { get; set; }

    [Required]
    [Column("itm_id")]
    public string ItemId { get; set; }

    [Required]
    [Column("tag_id")]
    public string TagId { get; set; }

    [Column("trs_type")]
    public string Type { get; set; }

    [Column("ref_no")]
    public string Reference { get; set; }


    [Column("created_by")]
    public string CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("action")]
    public string Action { get; set; }
    [Column("cycle_count")]

    public Item Item { get; set; }
    public Tag Tag { get; set; }
}
