using InventoryControl.Models;
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
    [MaxLength(36)]
    public string Id { get; set; }

    [Required]
    [Column("reader_id")]
    [MaxLength(30)]
    public string RdrId { get; set; }

    [Required]
    [Column("location_id")]
    [MaxLength(36)]
    public string LocationId { get; set; }

    [Required]
    [Column("rdr_name")]
    [MaxLength(50)]
    public string Name { get; set; }

    [Required]
    [Column("ip_address")]
    [MaxLength(20)]
    public string IpAddress { get; set; }

    [Required]
    [Column("search_mode")]
    [MaxLength(30)]
    public ReaderSearchMode SearchMode { get; set; } = ReaderSearchMode.DualTarget;

    [Required]
    [Column("session")]
    [MaxLength(10)]
    public ReaderSession Session { get; set; } = ReaderSession.S2;

    [Column("duplicate_scan_interval")]
    public int DuplicateScanInterval { get; set; } = 2;

    [Column("status")]
    [MaxLength(30)]
    public string Status { get; set; } // READY, OFFLINE, IN_USE

    [Required]
    [Column("created_by")]
    [MaxLength(50)]
    public string CreatedBy { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_by")]
    [MaxLength(50)]
    public string? UpdatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    [Column("isDelete")]
    public bool IsDelete { get; set; } = false;

    public Location LocationNavigation { get; set; }
}