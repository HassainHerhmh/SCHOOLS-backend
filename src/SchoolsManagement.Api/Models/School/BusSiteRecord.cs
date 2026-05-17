using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

/// <summary>مواقع الباصات والرسوم — جدول bus_sites.</summary>
[Table("bus_sites")]
public class BusSiteRecord
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [Column("site_name")]
    [MaxLength(500)]
    public string SiteName { get; set; } = string.Empty;

    [Column("fee_amount", TypeName = "decimal(14,2)")]
    public decimal FeeAmount { get; set; }

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }
}
