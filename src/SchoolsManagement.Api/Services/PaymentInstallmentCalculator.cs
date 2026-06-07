namespace SchoolsManagement.Api.Services;

public sealed class InstallmentSlotResult
{
    public int Index { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Due { get; set; }
    public decimal Paid { get; set; }
    public decimal Remaining { get; set; }
    public bool IsFullyPaid { get; set; }
}

public static class PaymentInstallmentCalculator
{
    public static int NormalizeCount(int count) => Math.Clamp(count, 1, 12);

    public static List<decimal> SplitAmount(decimal total, int count)
    {
        count = NormalizeCount(count);
        if (total <= 0)
        {
            return Enumerable.Repeat(0m, count).ToList();
        }

        var per = Math.Floor(total / count * 100m) / 100m;
        var installments = Enumerable.Repeat(per, count).ToList();
        var assigned = per * (count - 1);
        installments[count - 1] = Math.Round(total - assigned, 2, MidpointRounding.AwayFromZero);
        return installments;
    }

    public static string LabelFor(
        string feeKind,
        int index,
        int count,
        IReadOnlyList<string>? tuitionMonthLabels = null)
    {
        if (feeKind == "bus_fees" && count == 2)
        {
            return index == 1 ? "الفصل الأول" : "الفصل الثاني";
        }

        if (feeKind == "bus_fees")
        {
            return $"الفصل {index}";
        }

        if (feeKind == "school_fees" && tuitionMonthLabels is { Count: > 0 })
        {
            var idx = index - 1;
            if (idx >= 0 && idx < tuitionMonthLabels.Count && !string.IsNullOrWhiteSpace(tuitionMonthLabels[idx]))
            {
                return tuitionMonthLabels[idx];
            }
        }

        return $"قسط {index}";
    }

    public static List<InstallmentSlotResult> BuildSlots(
        decimal totalFees,
        int installmentsCount,
        decimal totalPaid,
        string feeKind,
        IReadOnlyList<string>? tuitionMonthLabels = null)
    {
        var count = NormalizeCount(installmentsCount);
        var dues = SplitAmount(totalFees, count);
        var remainingPaid = Math.Max(0, totalPaid);
        var slots = new List<InstallmentSlotResult>();

        for (var i = 0; i < count; i++)
        {
            var due = dues[i];
            var paid = Math.Min(due, remainingPaid);
            remainingPaid -= paid;
            var remaining = Math.Max(0, due - paid);
            slots.Add(new InstallmentSlotResult
            {
                Index = i + 1,
                Label = LabelFor(feeKind, i + 1, count, tuitionMonthLabels),
                Due = due,
                Paid = paid,
                Remaining = remaining,
                IsFullyPaid = remaining <= 0.009m
            });
        }

        return slots;
    }

    public static int RemainingInstallmentsCount(IEnumerable<InstallmentSlotResult> slots) =>
        slots.Count(s => s.Remaining > 0.009m);

    public static List<(string Label, decimal Amount)> SimulateReceiptAllocation(
        decimal totalFees,
        int installmentsCount,
        decimal totalPaidBefore,
        decimal receiptAmount,
        string feeKind,
        IReadOnlyList<string>? tuitionMonthLabels = null)
    {
        var maxDue = Math.Max(0, totalFees - totalPaidBefore);
        var applied = Math.Min(Math.Max(0, receiptAmount), maxDue);
        if (applied <= 0)
        {
            return [];
        }

        var before = BuildSlots(totalFees, installmentsCount, totalPaidBefore, feeKind, tuitionMonthLabels);
        var after = BuildSlots(totalFees, installmentsCount, totalPaidBefore + applied, feeKind, tuitionMonthLabels);
        var result = new List<(string Label, decimal Amount)>();

        for (var i = 0; i < before.Count; i++)
        {
            var delta = Math.Round(after[i].Paid - before[i].Paid, 2, MidpointRounding.AwayFromZero);
            if (delta > 0)
            {
                result.Add((after[i].Label, delta));
            }
        }

        return result;
    }

    public static string FormatAllocationNote(IEnumerable<(string Label, decimal Amount)> parts)
    {
        var items = parts
            .Where(p => p.Amount > 0)
            .Select(p => $"{p.Label}: {p.Amount:N2}")
            .ToList();
        return items.Count == 0 ? string.Empty : string.Join("، ", items);
    }
}
