using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("parents_classes")]
public class ParentsClassPublishRecord
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Level { get; set; } = string.Empty;

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("tuition_fees", TypeName = "decimal(18,2)")]
    public decimal TuitionFees { get; set; }

    [Column("uniform_fees", TypeName = "decimal(18,2)")]
    public decimal UniformFees { get; set; }

    [Column("bus_fees", TypeName = "decimal(18,2)")]
    public decimal BusFees { get; set; }

    [Column("books_fees", TypeName = "decimal(18,2)")]
    public decimal BooksFees { get; set; }

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }
}
