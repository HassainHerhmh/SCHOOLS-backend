using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Services;

/// <summary>نفس منطق تقارير الطلاب في الواجهة (المستحق، النقد، الخصم، المتبقي).</summary>
public static class ParentsStudentReportCalculator
{
    public static ParentsStudentReportIngestDto FromStudent(StudentRecord student, decimal discountAmount)
    {
        var ps = student.PaidSchoolFees ?? 0;
        var pu = student.PaidUniformFees ?? 0;
        var pbk = student.PaidBooksFees ?? 0;
        var pbs = student.PaidBusFees ?? 0;
        var feeSum = student.SchoolFees + student.UniformFees + student.BooksFees + student.BusFees;
        var allocTotal = ps + pu + pbk + pbs;
        var total = Math.Max(student.TotalAmount, feeSum);
        var paidField = student.PaidAmount;
        var disc = Math.Max(0, discountAmount);
        var paidCash = allocTotal > 0 ? allocTotal : paidField;
        var remaining = Math.Max(0, total - disc - paidCash);

        return new ParentsStudentReportIngestDto
        {
            StudentId = student.Id,
            ParentPhone = student.ParentPhone ?? student.Phone,
            Name = student.Name,
            Level = student.Level,
            Section = student.Section,
            SchoolFees = student.SchoolFees,
            UniformFees = student.UniformFees,
            BooksFees = student.BooksFees,
            BusFees = student.BusFees,
            PaidSchoolFees = ps,
            PaidUniformFees = pu,
            PaidBooksFees = pbk,
            PaidBusFees = pbs,
            TotalAmount = total,
            PaidCashAmount = paidCash,
            DiscountAmount = disc,
            RemainingAmount = remaining
        };
    }
}
