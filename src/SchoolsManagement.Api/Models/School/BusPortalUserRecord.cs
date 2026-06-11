using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

/// <summary>مستخدمو بوابة الباصات — مخزَّن محلياً في جدول bus_users.</summary>
[Table("bus_users")]
public class BusPortalUserRecord
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [Column("full_name")]
    [MaxLength(500)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Column("phone_number")]
    [MaxLength(40)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [Column("username")]
    [MaxLength(120)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Column("password")]
    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
}
