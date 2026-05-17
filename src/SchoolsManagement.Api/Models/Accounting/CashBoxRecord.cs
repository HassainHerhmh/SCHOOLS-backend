using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolsManagement.Api.Models.Accounting;

[Table("cash_boxes")]
public class CashBoxRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("name_ar")]
    [MaxLength(500)]
    public string NameAr { get; set; } = string.Empty;

    [Column("name_en")]
    [MaxLength(500)]
    public string NameEn { get; set; } = string.Empty;

    [Column("code")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Column("cash_box_group_id")]
    public int? CashBoxGroupId { get; set; }

    [Column("parent_account_id")]
    public int? ParentAccountId { get; set; }

    [Column("account_id")]
    public int? AccountId { get; set; }

    [Column("branch_id")]
    public int? BranchId { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }
}
