using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_Reader")]
[Index(nameof(RdrId), IsUnique = true)]
public class Reader
{
    [Key]
    [Column("id")]
    public string Id { get; set; }

    [Required]
    [Column("rdr_id")]
    public string RdrId { get; set; }

    [Required]
    [Column("loc_id")]
    public string LocationId { get; set; }

    [Required]
    [Column("rdr_name")]
    public string Name { get; set; }

    [Required]
    [Column("ip_address")]
    public string IpAddress { get; set; }

    [Column("status")]
    public string Status { get; set; } // READY, OFFLINE, IN_USE

    [Required]
    [Column("created_by")]
    public string CreatedBy { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    [Column("isDelete")]
    public bool IsDelete { get; set; } = false;

    public Location LocationNavigation { get; set; }
}