using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("students")]
public class StudentRecord
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Phone { get; set; }

    [Column("parent_phone")]
    [MaxLength(40)]
    public string? ParentPhone { get; set; }

    [MaxLength(250)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string Level { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Section { get; set; } = string.Empty;

    [Column("school_fees", TypeName = "decimal(18,2)")]
    public decimal SchoolFees { get; set; }

    [Column("uniform_fees", TypeName = "decimal(18,2)")]
    public decimal UniformFees { get; set; }

    [Column("books_fees", TypeName = "decimal(18,2)")]
    public decimal BooksFees { get; set; }

    [Column("bus_fees", TypeName = "decimal(18,2)")]
    public decimal BusFees { get; set; }

    [Column("total_amount", TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column("paid_amount", TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    [Column("remaining_amount", TypeName = "decimal(18,2)")]
    public decimal RemainingAmount { get; set; }

    [Column("paid_school_fees", TypeName = "decimal(18,2)")]
    public decimal? PaidSchoolFees { get; set; }

    [Column("paid_uniform_fees", TypeName = "decimal(18,2)")]
    public decimal? PaidUniformFees { get; set; }

    [Column("paid_books_fees", TypeName = "decimal(18,2)")]
    public decimal? PaidBooksFees { get; set; }

    [Column("paid_bus_fees", TypeName = "decimal(18,2)")]
    public decimal? PaidBusFees { get; set; }

    [MaxLength(40)]
    public string? Gender { get; set; }

    [MaxLength(40)]
    public string Status { get; set; } = "active";

    [Column("bus_site_id")]
    public Guid? BusSiteId { get; set; }

    [Column("bus_site_name")]
    [MaxLength(500)]
    public string? BusSiteName { get; set; }

    [Column("bus_driver_id")]
    public Guid? BusDriverId { get; set; }

    [Column("bus_driver_name")]
    [MaxLength(500)]
    public string? BusDriverName { get; set; }

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
