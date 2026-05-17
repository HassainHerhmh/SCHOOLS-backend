namespace SchoolsManagement.Api.Models.School;

public class UpdateStudentPaymentRequest
{
    public decimal PaidSchoolFees { get; set; }
    public decimal PaidUniformFees { get; set; }
    public decimal PaidBooksFees { get; set; }
    public decimal PaidBusFees { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}
