using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("sections")]
public class SchoolSection
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(250)]
    public string Name { get; set; } = string.Empty;

    [Column("class_id")]
    public Guid ClassId { get; set; }

    [ForeignKey(nameof(ClassId))]
    public GradeClass? Class { get; set; }

    [Column("teacher_id")]
    public Guid? TeacherId { get; set; }

    [Column("teacher_name")]
    [MaxLength(500)]
    public string? TeacherName { get; set; }

    [Column("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
