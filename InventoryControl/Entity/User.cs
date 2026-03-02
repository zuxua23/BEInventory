using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_User")]
[Index(nameof(UserId), IsUnique = true)]

public class User
{
    [Key]
    [Column("id")]
    public string Id { get; set; }

    [Required]
    [Column("usr_id")]
    public string UserId { get; set; }

    [Column("usr_fullname")]
    public string Fullname { get; set; }

    [Required]
    [Column("usr_name")]
    public string Username { get; set; }

    [Required]
    [Column("usr_password")]
    public string Password { get; set; }

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

    [Column("isActive")]
    public bool IsActive { get; set; } = true;
    [Column("isDelete")]
    public bool IsDelete { get; set; } = false;

    public ICollection<User_Role> UserRoles { get; set; }

}
