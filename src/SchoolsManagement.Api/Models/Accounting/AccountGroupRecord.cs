using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.Accounting;

[Table("account_groups")]
public class AccountGroupRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>رمز مجموعة الحساب (رقم كما في واجهة الإعداد).</summary>
    public int Code { get; set; }

    [Column("name_ar")]
    [MaxLength(500)]
    public string NameAr { get; set; } = string.Empty;

    [Column("name_en")]
    [MaxLength(500)]
    public string NameEn { get; set; } = string.Empty;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("branch_id")]
    public int? BranchId { get; set; }
}
