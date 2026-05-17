using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.School;

[Table("employee_absence_settings")]
public class EmployeeAbsenceSettingRecord
{
    [Key]
    public int Id { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    [Column("deduction_with_excuse", TypeName = "decimal(9,2)")]
    public decimal DeductionWithExcuse { get; set; } = 10;

    [Column("deduction_without_excuse", TypeName = "decimal(9,2)")]
    public decimal DeductionWithoutExcuse { get; set; } = 20;

    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
