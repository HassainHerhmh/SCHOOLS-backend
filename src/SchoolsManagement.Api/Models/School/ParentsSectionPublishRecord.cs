using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("parents_sections")]
public class ParentsSectionPublishRecord
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [Column("class_id")]
    public Guid ClassId { get; set; }

    [Column("teacher_id")]
    public Guid? TeacherId { get; set; }

    [Column("teacher_name")]
    [MaxLength(300)]
    public string? TeacherName { get; set; }

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }
}
