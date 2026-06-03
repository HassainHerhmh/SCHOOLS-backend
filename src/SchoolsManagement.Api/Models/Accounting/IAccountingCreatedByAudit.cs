namespace SchoolsManagement.Api.Models.Accounting;

public interface IAccountingCreatedByAudit
{
    int? CreatedBy { get; set; }
    string? CreatedByUserId { get; set; }
    string? CreatedByName { get; set; }
}
