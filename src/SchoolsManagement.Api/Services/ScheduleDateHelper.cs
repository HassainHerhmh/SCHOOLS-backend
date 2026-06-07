namespace SchoolsManagement.Api.Services;

public static class ScheduleDateHelper
{
    private static readonly string[] ArabicWeekDays =
    [
        "الأحد",
        "الاثنين",
        "الثلاثاء",
        "الأربعاء",
        "الخميس",
        "الجمعة",
        "السبت"
    ];

    public static bool TryParse(string? value, string? alt, out DateOnly date)
    {
        var raw = (value ?? alt ?? string.Empty).Trim();
        if (DateOnly.TryParse(raw, out date))
        {
            return true;
        }

        if (DateTime.TryParse(raw, out var dt))
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }

        date = default;
        return false;
    }

    public static string ArabicDayName(DateOnly date) => ArabicWeekDays[(int)date.DayOfWeek];

    public static string ToApiString(DateOnly date) => date.ToString("yyyy-MM-dd");
}
