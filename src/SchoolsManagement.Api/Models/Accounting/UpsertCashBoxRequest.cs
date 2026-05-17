using System.ComponentModel.DataAnnotations;

namespace SchoolsManagement.Api.Models.Accounting;

public class UpsertCashBoxRequest
{
    [Required]
    [MaxLength(500)]
    public string NameAr { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? NameEn { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public int? CashBoxGroupId { get; set; }

    public int? ParentAccountId { get; set; }

    public int? AccountId { get; set; }

    public int? BranchId { get; set; }

    public int? CreatedBy { get; set; }
}
