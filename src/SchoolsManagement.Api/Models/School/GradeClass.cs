using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("classes")]
public class GradeClass
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(250)]
    public string Name { get; set; } = string.Empty;

    public int Level { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("tuition_fees", TypeName = "decimal(18,2)")]
    public decimal TuitionFees { get; set; }

    [Column("uniform_fees", TypeName = "decimal(18,2)")]
    public decimal UniformFees { get; set; }

    [Column("books_fees", TypeName = "decimal(18,2)")]
    public decimal BooksFees { get; set; }

    [Column("bus_fees", TypeName = "decimal(18,2)")]
    public decimal BusFees { get; set; }

    [Column("default_min_pass_score", TypeName = "decimal(18,2)")]
    public decimal DefaultMinPassScore { get; set; } = 50;

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<SchoolSection> Sections { get; set; } = new List<SchoolSection>();
}
