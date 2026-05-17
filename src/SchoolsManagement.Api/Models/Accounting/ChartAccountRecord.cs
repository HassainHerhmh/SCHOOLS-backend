using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.Accounting;

/// <summary>Chart of accounts stored in dbo.accountss; id is the account key used by the tree.</summary>
[Table("accountss")]
public class ChartAccountRecord
{
    /// <summary>معرّف الحساب المعروض للواجهة (JSON: id؛ يستخدم في parent_id).</summary>
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [Column("code")]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Column("name_ar")]
    [MaxLength(500)]
    public string NameAr { get; set; } = string.Empty;

    [Column("name_en")]
    [MaxLength(500)]
    public string NameEn { get; set; } = string.Empty;

    [Column("parent_id")]
    public int? ParentId { get; set; }

    [Column("account_group_id")]
    public int? AccountGroupId { get; set; }

    [Column("account_level")]
    [MaxLength(100)]
    public string AccountLevel { get; set; } = string.Empty;

    [Column("financial_statement_id")]
    [MaxLength(250)]
    public string? FinancialStatementId { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("created_by")]
    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    [Column("branch_id")]
    [MaxLength(500)]
    public string? BranchId { get; set; }
}
