namespace SchoolsManagement.Api.Security;

/// <summary>مفاتيح صلاحيات الصفحات — يجب أن تتطابق مع الواجهة.</summary>
public static class PermissionCatalog
{
    public const string ControlRoom = "control_room";
    public const string SystemUsers = "system_users";
    public const string Permissions = "permissions";
    public const string FinancialAnnualReport = "financial_annual_report";
    public const string EmployeeAnnualReport = "employee_annual_report";
    public const string SystemBackup = "system_backup";
    public const string TransferApprovals = "transfer_approvals";
    public const string AccountingManagement = "accounting_management";
    public const string Students = "students";
    public const string Classes = "classes";
    public const string StudentReports = "student_reports";
    public const string StudentDiscounts = "student_discounts";
    public const string StudentAttendance = "student_attendance";
    public const string StudentGrades = "student_grades";
    public const string TopStudents = "top_students";
    public const string FailingStudents = "failing_students";
    public const string Employees = "employees";
    public const string EmployeeDeduction = "employee_deduction";
    public const string EmployeePreparation = "employee_preparation";
    public const string EmployeeBonus = "employee_bonus";
    public const string AccountingEmployees = "accounting_employees";
    public const string BusUsers = "bus_users";
    public const string BusSites = "bus_sites";
    public const string ParentAppSync = "parent_app_sync";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(ControlRoom, "الرئيسية", "لوحة التحكم"),
        new(SystemUsers, "المستخدمون", "إدارة حسابات الدخول"),
        new(Permissions, "صلاحيات", "تعيين صلاحيات الصفحات للمستخدمين"),
        new(FinancialAnnualReport, "التقرير المالي السنوي", "تقارير"),
        new(EmployeeAnnualReport, "التقرير السنوي (خصومات/مكافآت)", "تقارير"),
        new(SystemBackup, "النسخ الاحتياطي", "النظام"),
        new(TransferApprovals, "اعتماد الحوالات", "النظام"),
        new(AccountingManagement, "إدارة الحسابات", "محاسبة"),
        new(Students, "شؤون الطلاب", "الطلاب"),
        new(Classes, "الفصول والشعب", "الطلاب"),
        new(StudentReports, "تقارير الطلاب", "الطلاب"),
        new(StudentDiscounts, "خصومات الطلاب", "الطلاب"),
        new(StudentAttendance, "حضور الطلاب", "الطلاب"),
        new(StudentGrades, "درجات الطلاب", "الطلاب"),
        new(TopStudents, "الطلاب المتفوقون", "الطلاب"),
        new(FailingStudents, "الطلاب الراسبون", "الطلاب"),
        new(Employees, "الموظفين", "الموظفين"),
        new(EmployeeDeduction, "خصومات الموظفين", "الموظفين"),
        new(EmployeePreparation, "تحضير الموظفين", "الموظفين"),
        new(EmployeeBonus, "مكافآت الموظفين", "الموظفين"),
        new(AccountingEmployees, "رواتب الموظفين", "الموظفين"),
        new(BusUsers, "الباصات — المستخدمون", "الباصات"),
        new(BusSites, "الباصات — إدارة المواقع", "الباصات"),
        new(ParentAppSync, "مزامنة التطبيقات", "مزامنة التطبيقات"),
    ];

    public static IReadOnlySet<string> AllKeys { get; } =
        All.Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsValidKey(string? key) =>
        !string.IsNullOrWhiteSpace(key) && AllKeys.Contains(key.Trim());
}

public sealed record PermissionDefinition(string Key, string Label, string Group);
