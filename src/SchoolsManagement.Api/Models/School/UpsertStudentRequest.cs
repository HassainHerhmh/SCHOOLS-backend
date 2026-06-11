namespace SchoolsManagement.Api.Models.School;

/// <summary>طلب إنشاء/تحديث طالب يطابق الحقول المرسلة من Angular (snake_case عبر سياسة JSON).</summary>
public class UpsertStudentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ParentPhone { get; set; }
    public string? Email { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public decimal SchoolFees { get; set; }
    public decimal UniformFees { get; set; }
    public decimal BooksFees { get; set; }
    public decimal BusFees { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Gender { get; set; }
    public Guid? BusSiteId { get; set; }
    public string? BusSiteName { get; set; }
    public Guid? BusDriverId { get; set; }
    public string? BusDriverName { get; set; }
}
