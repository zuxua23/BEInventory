using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryControl.Entity;

[Table("tb_User_Role")]
public class User_Role
{
    [Key]
    [Column("id")]
    public string Id { get; set; }

    [Required]
    [Column("role_id")]
    public string RoleId { get; set; }

    [Required]
    [Column("user_id")]
    public string UserId { get; set; }

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    [Column("updated_at")]

    public DateTime? UpdatedAt { get; set; }

    public Role Role { get; set; }
    public User User { get; set; }

}
