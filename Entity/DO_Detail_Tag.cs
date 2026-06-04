using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_DO_Detail_Tag")]
public class DODetailTag
{
    [Key]
    [Column("id")]
    [MaxLength(36)]
    public string Id { get; set; }

    [Required]
    [Column("do_detail_id")]
    [MaxLength(36)]
    public string DoDetailId { get; set; }

    [Required]
    [Column("tag_id")]
    [MaxLength(36)]
    public string TagId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public DODetail DODetail { get; set; }
    public Tag Tag { get; set; }
}