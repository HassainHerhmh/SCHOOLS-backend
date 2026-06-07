using System.Text.Json;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Services;

public static class ParentsInstallmentSyncHelper
{
    private static readonly string[] DefaultSchoolYearMonths =
    [
        "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر", "يناير", "فبراير",
        "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس"
    ];

    public static List<ParentsInstallmentIngestDto> BuildForStudent(
        StudentRecord student,
        PaymentInstallmentSettingsRecord settings)
    {
        var result = new List<ParentsInstallmentIngestDto>();
        result.AddRange(BuildForFeeKind(student, settings, "school_fees"));
        result.AddRange(BuildForFeeKind(student, settings, "bus_fees"));
        return result;
    }

    private static IEnumerable<ParentsInstallmentIngestDto> BuildForFeeKind(
        StudentRecord student,
        PaymentInstallmentSettingsRecord settings,
        string feeKind)
    {
        var totalFees = feeKind == "bus_fees" ? student.BusFees : student.SchoolFees;
        var totalPaid = feeKind == "bus_fees"
            ? student.PaidBusFees ?? 0
            : student.PaidSchoolFees ?? 0;
        var count = feeKind == "bus_fees"
            ? settings.BusInstallmentsCount
            : settings.TuitionInstallmentsCount;
        var monthLabels = feeKind == "school_fees"
            ? ParseTuitionMonthLabels(settings, count)
            : null;

        var slots = PaymentInstallmentCalculator.BuildSlots(totalFees, count, totalPaid, feeKind, monthLabels);
        return slots.Select(s => new ParentsInstallmentIngestDto
        {
            StudentId = student.Id,
            FeeKind = feeKind,
            SlotIndex = s.Index,
            Label = s.Label,
            Due = s.Due,
            Paid = s.Paid,
            Remaining = s.Remaining,
            IsFullyPaid = s.IsFullyPaid
        });
    }

    private static List<string> DefaultTuitionMonthLabels(int count)
    {
        count = PaymentInstallmentCalculator.NormalizeCount(count);
        return DefaultSchoolYearMonths.Take(count).ToList();
    }

    private static List<string> NormalizeTuitionMonthLabels(int count, IEnumerable<string>? labels)
    {
        count = PaymentInstallmentCalculator.NormalizeCount(count);
        var defaults = DefaultTuitionMonthLabels(count);
        var source = labels?.Select(x => (x ?? string.Empty).Trim()).ToList() ?? [];
        return Enumerable.Range(0, count)
            .Select(i => i < source.Count && !string.IsNullOrWhiteSpace(source[i]) ? source[i] : defaults[i])
            .ToList();
    }

    private static List<string> ParseTuitionMonthLabels(PaymentInstallmentSettingsRecord settings, int count)
    {
        count = PaymentInstallmentCalculator.NormalizeCount(count);
        if (!string.IsNullOrWhiteSpace(settings.TuitionMonthLabelsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(settings.TuitionMonthLabelsJson);
                if (parsed is { Count: > 0 })
                {
                    return NormalizeTuitionMonthLabels(count, parsed);
                }
            }
            catch
            {
                /* fallback */
            }
        }

        return DefaultTuitionMonthLabels(count);
    }
}
