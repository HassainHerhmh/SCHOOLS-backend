using System.ComponentModel.DataAnnotations;

namespace SchoolsManagement.Api.Models.Accounting;

public class UpsertChartAccountCreateRequest
{
    /// <summary>إن وُجد يُستخدم كـ id المنطقي؛ وإلا يُحسب تلقائيًا من Max(id)+1.</summary>
    public int? Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string NameAr { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? NameEn { get; set; }

    public int? ParentId { get; set; }

    public int? AccountGroupId { get; set; }

    [Required]
    [MaxLength(100)]
    public string AccountLevel { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? FinancialStatementId { get; set; }

    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    [MaxLength(500)]
    public string? BranchId { get; set; }
}

public class UpsertAccountGroupRequest
{
    [Required]
    public int Code { get; set; }

    [Required]
    [MaxLength(500)]
    public string NameAr { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? NameEn { get; set; }

    public int SortOrder { get; set; }

    public int? BranchId { get; set; }
}
