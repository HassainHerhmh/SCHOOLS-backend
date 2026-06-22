using System.Text.RegularExpressions;

namespace SchoolsManagement.Api.Data;

/// <summary>أسماء عربية لجداول وأعمدة النسخ الاحتياطي.</summary>
public static class DatabaseBackupCatalog
{
    private static readonly HashSet<string> ExcludedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "__EFMigrationsHistory"
    };

    /// <summary>ترتيب العرض — يُدمج مع أي جدول موجود فعلياً في dbo.</summary>
    public static readonly string[] PreferredTableOrder =
    [
        "students", "classes", "sections", "attendance",
        "student_payments", "student_discounts", "student_discount_applications",
        "subjects", "exams", "grade_rules", "grades",
        "employees", "employee_monthly_accounts", "employee_absence_settings", "employee_monthly_processes",
        "account_groups", "accountss", "currencies", "currency_exchanges",
        "journal_types", "payment_types", "receipt_types",
        "cashbox_groups", "cash_boxes", "bank_groups", "banks", "transit_accounts_settings",
        "receipt_vouchers", "payment_vouchers", "journal_entries",
        "transfer_approval_requests",
        "bus_users", "bus_sites",
        "sync_checkpoints",
        "parents_students_summary", "parents_classes", "parents_sections",
        "parents_attendance_summary", "parents_student_reports",
        "user_page_permissions",
        "AspNetUsers", "AspNetRoles", "AspNetUserRoles",
        "AspNetUserClaims", "AspNetUserLogins", "AspNetUserTokens", "AspNetRoleClaims"
    ];

    private static readonly Dictionary<string, string> TableLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["students"] = "الطلاب",
        ["classes"] = "الصفوف",
        ["sections"] = "الشعب",
        ["attendance"] = "الحضور والغياب",
        ["employees"] = "الموظفون",
        ["employee_monthly_accounts"] = "حسابات الرواتب الشهرية",
        ["employee_absence_settings"] = "إعدادات غياب الموظفين",
        ["employee_monthly_processes"] = "عمليات الرواتب الشهرية",
        ["account_groups"] = "مجموعات الحسابات",
        ["accountss"] = "دليل الحسابات",
        ["currencies"] = "العملات",
        ["currency_exchanges"] = "صرف العملات",
        ["journal_types"] = "أنواع القيود",
        ["payment_types"] = "أنواع الصرف",
        ["receipt_types"] = "أنواع القبض",
        ["cashbox_groups"] = "مجموعات الصناديق",
        ["cash_boxes"] = "الصناديق",
        ["bank_groups"] = "مجموعات البنوك",
        ["banks"] = "البنوك",
        ["transit_accounts_settings"] = "إعدادات حسابات العبور",
        ["receipt_vouchers"] = "سندات القبض",
        ["payment_vouchers"] = "سندات الصرف",
        ["journal_entries"] = "قيود اليومية",
        ["student_payments"] = "مدفوعات الطلاب",
        ["student_discounts"] = "خصومات الطلاب",
        ["student_discount_applications"] = "تطبيقات الخصومات على الطلاب",
        ["subjects"] = "المواد الدراسية",
        ["exams"] = "الاختبارات",
        ["grade_rules"] = "قواعد النجاح",
        ["grades"] = "درجات الطلاب",
        ["transfer_approval_requests"] = "طلبات اعتماد الحوالات",
        ["bus_users"] = "مستخدمو الحافلات",
        ["bus_sites"] = "مواقع الحافلات",
        ["sync_checkpoints"] = "نقاط مزامنة أولياء الأمور",
        ["user_page_permissions"] = "صلاحيات صفحات المستخدمين",
        ["parents_students_summary"] = "ملخص طلاب تطبيق أولياء الأمور",
        ["parents_classes"] = "صفوف تطبيق أولياء الأمور",
        ["parents_sections"] = "شعب تطبيق أولياء الأمور",
        ["parents_attendance_summary"] = "ملخص حضور تطبيق أولياء الأمور",
        ["parents_student_reports"] = "تقارير طلاب تطبيق أولياء الأمور",
        ["AspNetUsers"] = "مستخدمو النظام",
        ["AspNetRoles"] = "أدوار النظام",
        ["AspNetUserRoles"] = "ربط المستخدمين بالأدوار",
        ["AspNetUserClaims"] = "صلاحيات المستخدمين (Claims)",
        ["AspNetUserLogins"] = "تسجيلات دخول المستخدمين",
        ["AspNetUserTokens"] = "رموز المستخدمين",
        ["AspNetRoleClaims"] = "صلاحيات الأدوار",
        ["payments"] = "مدفوعات الطلاب"
    };

    private static readonly Dictionary<string, string> SqlTableNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["payments"] = "student_payments"
    };

    private static readonly Dictionary<string, string> ColumnLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "المعرف",
        ["Id"] = "المعرف",
        ["name"] = "الاسم",
        ["Name"] = "الاسم",
        ["user_name"] = "اسم المستخدم",
        ["UserName"] = "اسم المستخدم",
        ["normalized_user_name"] = "اسم المستخدم (موحّد)",
        ["NormalizedUserName"] = "اسم المستخدم (موحّد)",
        ["email"] = "البريد الإلكتروني",
        ["Email"] = "البريد الإلكتروني",
        ["normalized_email"] = "البريد (موحّد)",
        ["NormalizedEmail"] = "البريد (موحّد)",
        ["email_confirmed"] = "البريد مؤكد",
        ["EmailConfirmed"] = "البريد مؤكد",
        ["password_hash"] = "كلمة المرور (مشفّرة)",
        ["PasswordHash"] = "كلمة المرور (مشفّرة)",
        ["security_stamp"] = "ختم الأمان",
        ["SecurityStamp"] = "ختم الأمان",
        ["concurrency_stamp"] = "ختم التزامن",
        ["ConcurrencyStamp"] = "ختم التزامن",
        ["phone"] = "الهاتف",
        ["Phone"] = "الهاتف",
        ["phone_number"] = "رقم الهاتف",
        ["PhoneNumber"] = "رقم الهاتف",
        ["phone_number_confirmed"] = "الهاتف مؤكد",
        ["PhoneNumberConfirmed"] = "الهاتف مؤكد",
        ["two_factor_enabled"] = "التحقق بخطوتين",
        ["TwoFactorEnabled"] = "التحقق بخطوتين",
        ["lockout_end"] = "نهاية الحظر",
        ["LockoutEnd"] = "نهاية الحظر",
        ["lockout_enabled"] = "تفعيل الحظر",
        ["LockoutEnabled"] = "تفعيل الحظر",
        ["access_failed_count"] = "محاولات فاشلة",
        ["AccessFailedCount"] = "محاولات فاشلة",
        ["user_type"] = "نوع المستخدم",
        ["UserType"] = "نوع المستخدم",
        ["permissions_json"] = "الصلاحيات (JSON)",
        ["PermissionsJson"] = "الصلاحيات (JSON)",
        ["parent_phone"] = "هاتف ولي الأمر",
        ["level"] = "المرحلة / الصف",
        ["Level"] = "المرحلة / الصف",
        ["section"] = "الشعبة",
        ["Section"] = "الشعبة",
        ["school_fees"] = "رسوم المدرسة",
        ["uniform_fees"] = "رسوم الزي",
        ["books_fees"] = "رسوم الكتب",
        ["bus_fees"] = "رسوم الحافلة",
        ["total_amount"] = "المبلغ الإجمالي",
        ["paid_amount"] = "المبلغ المدفوع",
        ["remaining_amount"] = "المبلغ المتبقي",
        ["paid_school_fees"] = "مدفوع رسوم المدرسة",
        ["paid_uniform_fees"] = "مدفوع رسوم الزي",
        ["paid_books_fees"] = "مدفوع رسوم الكتب",
        ["paid_bus_fees"] = "مدفوع رسوم الحافلة",
        ["gender"] = "الجنس",
        ["status"] = "الحالة",
        ["bus_site_id"] = "معرف موقع الحافلة",
        ["bus_site_name"] = "موقع الحافلة",
        ["bus_location_url"] = "رابط موقع الطالب",
        ["bus_driver_id"] = "معرف سائق الباص",
        ["bus_driver_name"] = "سائق الباص",
        ["created_at"] = "تاريخ الإنشاء",
        ["updated_at"] = "تاريخ التحديث",
        ["synced_at"] = "تاريخ المزامنة",
        ["student_id"] = "معرف الطالب",
        ["student_name"] = "اسم الطالب",
        ["class_id"] = "معرف الصف",
        ["section_id"] = "معرف الشعبة",
        ["subject_id"] = "معرف المادة",
        ["subject_name"] = "اسم المادة",
        ["exam_id"] = "معرف الاختبار",
        ["exam_name"] = "اسم الاختبار",
        ["exam_type"] = "نوع الاختبار",
        ["exam_date"] = "تاريخ الاختبار",
        ["exam_title"] = "عنوان الاختبار",
        ["title"] = "العنوان",
        ["max_score"] = "الدرجة العظمى",
        ["score"] = "الدرجة",
        ["percentage"] = "النسبة المئوية",
        ["academic_year"] = "السنة الدراسية",
        ["semester"] = "الفصل الدراسي",
        ["min_pass_score"] = "حد النجاح",
        ["default_min_pass_score"] = "حد النجاح الافتراضي",
        ["amount"] = "المبلغ",
        ["payment_date"] = "تاريخ الدفع",
        ["receipt_number"] = "رقم الإيصال",
        ["school_fees_paid"] = "رسوم مدرسة مدفوعة",
        ["uniform_fees_paid"] = "رسوم زي مدفوعة",
        ["bus_fees_paid"] = "رسوم حافلة مدفوعة",
        ["books_fees_paid"] = "رسوم كتب مدفوعة",
        ["payment_type"] = "نوع الدفع",
        ["notes"] = "ملاحظات",
        ["description"] = "الوصف",
        ["is_active"] = "نشط",
        ["discount_id"] = "معرف الخصم",
        ["discount_name"] = "اسم الخصم",
        ["applied_at"] = "تاريخ التطبيق",
        ["created_by"] = "أنشئ بواسطة",
        ["parent_name"] = "اسم ولي الأمر",
        ["payment_method"] = "طريقة الدفع",
        ["transfer_no"] = "رقم الحوالة",
        ["bank_id"] = "معرف البنك",
        ["currency_id"] = "معرف العملة",
        ["approved_at"] = "تاريخ الاعتماد",
        ["approved_by"] = "اعتمد بواسطة",
        ["teacher_id"] = "معرف المعلم",
        ["teacher_name"] = "اسم المعلم",
        ["display_order"] = "ترتيب العرض",
        ["tuition_fees"] = "رسوم دراسية",
        ["date"] = "التاريخ",
        ["employee_id"] = "معرف الموظف",
        ["chart_account_id"] = "معرف حساب الدليل",
        ["year"] = "السنة",
        ["month"] = "الشهر",
        ["group_id"] = "معرف المجموعة",
        ["account_id"] = "معرف الحساب",
        ["code"] = "الرمز",
        ["symbol"] = "الرمز",
        ["exchange_rate"] = "سعر الصرف",
        ["is_default"] = "افتراضي",
        ["entry_number"] = "رقم القيد",
        ["entry_date"] = "تاريخ القيد",
        ["reference"] = "المرجع",
        ["debit"] = "مدين",
        ["credit"] = "دائن",
        ["voucher_number"] = "رقم السند",
        ["voucher_date"] = "تاريخ السند",
        ["posted_at"] = "تاريخ الترحيل",
        ["page_key"] = "مفتاح الصفحة",
        ["allowed"] = "مسموح",
        ["user_id"] = "معرف المستخدم",
        ["role_id"] = "معرف الدور",
        ["claim_type"] = "نوع الصلاحية",
        ["claim_value"] = "قيمة الصلاحية",
        ["login_provider"] = "مزود الدخول",
        ["provider_key"] = "مفتاح المزود",
        ["provider_display_name"] = "اسم المزود",
        ["entity_type"] = "نوع الكيان",
        ["entity_id"] = "معرف الكيان",
        ["last_synced_at"] = "آخر مزامنة",
        ["username"] = "اسم المستخدم",
        ["password"] = "كلمة المرور (مشفّرة)",
        ["site_name"] = "اسم الموقع",
        ["address"] = "العنوان",
        ["latitude"] = "خط العرض",
        ["longitude"] = "خط الطول",
        ["financial_summary_json"] = "ملخص مالي (JSON)",
        ["attendance_summary_json"] = "ملخص حضور (JSON)",
        ["grades_summary_json"] = "ملخص درجات (JSON)"
    };

    private static readonly Dictionary<string, string> TokenLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["student"] = "الطالب",
        ["students"] = "الطلاب",
        ["class"] = "الصف",
        ["classes"] = "الصفوف",
        ["section"] = "الشعبة",
        ["sections"] = "الشعب",
        ["parent"] = "ولي الأمر",
        ["phone"] = "الهاتف",
        ["email"] = "البريد",
        ["name"] = "الاسم",
        ["amount"] = "المبلغ",
        ["paid"] = "المدفوع",
        ["fees"] = "الرسوم",
        ["school"] = "المدرسة",
        ["uniform"] = "الزي",
        ["bus"] = "الحافلة",
        ["books"] = "الكتب",
        ["total"] = "الإجمالي",
        ["remaining"] = "المتبقي",
        ["payment"] = "الدفع",
        ["receipt"] = "الإيصال",
        ["discount"] = "الخصم",
        ["applied"] = "التطبيق",
        ["created"] = "الإنشاء",
        ["updated"] = "التحديث",
        ["synced"] = "المزامنة",
        ["status"] = "الحالة",
        ["date"] = "التاريخ",
        ["type"] = "النوع",
        ["number"] = "الرقم",
        ["notes"] = "ملاحظات",
        ["approved"] = "الاعتماد",
        ["transfer"] = "الحوالة",
        ["bank"] = "البنك",
        ["currency"] = "العملة",
        ["employee"] = "الموظف",
        ["monthly"] = "الشهري",
        ["account"] = "الحساب",
        ["accounts"] = "الحسابات",
        ["chart"] = "الدليل",
        ["group"] = "المجموعة",
        ["journal"] = "القيد",
        ["entry"] = "القيد",
        ["voucher"] = "السند",
        ["cash"] = "الصندوق",
        ["box"] = "الصندوق",
        ["transit"] = "العبور",
        ["settings"] = "الإعدادات",
        ["permissions"] = "الصلاحيات",
        ["page"] = "الصفحة",
        ["user"] = "المستخدم",
        ["role"] = "الدور",
        ["claim"] = "الصلاحية",
        ["login"] = "الدخول",
        ["provider"] = "المزود",
        ["token"] = "الرمز",
        ["checkpoint"] = "نقطة المزامنة",
        ["sync"] = "المزامنة",
        ["attendance"] = "الحضور",
        ["exam"] = "الاختبار",
        ["exams"] = "الاختبارات",
        ["subject"] = "المادة",
        ["subjects"] = "المواد",
        ["grade"] = "الدرجة",
        ["grades"] = "الدرجات",
        ["rule"] = "القاعدة",
        ["rules"] = "القواعد",
        ["min"] = "الحد الأدنى",
        ["pass"] = "النجاح",
        ["score"] = "الدرجة",
        ["max"] = "العظمى",
        ["academic"] = "الدراسي",
        ["year"] = "السنة",
        ["semester"] = "الفصل",
        ["teacher"] = "المعلم",
        ["display"] = "العرض",
        ["order"] = "الترتيب",
        ["tuition"] = "الدراسية",
        ["financial"] = "المالي",
        ["summary"] = "الملخص",
        ["report"] = "التقرير",
        ["reports"] = "التقارير",
        ["parents"] = "أولياء الأمور",
        ["approval"] = "الاعتماد",
        ["request"] = "الطلب",
        ["requests"] = "الطلبات",
        ["site"] = "الموقع",
        ["sites"] = "المواقع",
        ["active"] = "نشط",
        ["default"] = "الافتراضي",
        ["exchange"] = "الصرف",
        ["rate"] = "السعر",
        ["debit"] = "مدين",
        ["credit"] = "دائن",
        ["posted"] = "الترحيل",
        ["reference"] = "المرجع",
        ["level"] = "المستوى",
        ["gender"] = "الجنس",
        ["json"] = "(بيانات)",
        ["id"] = "المعرف",
        ["at"] = "في",
        ["by"] = "بواسطة",
        ["no"] = "الرقم",
        ["is"] = "",
        ["has"] = "",
        ["asp"] = "",
        ["net"] = ""
    };

    public static bool IsExcludedTable(string tableName) =>
        ExcludedTables.Contains(tableName);

    public static string ResolveSqlTableName(string tableKey) =>
        SqlTableNames.TryGetValue(tableKey, out var sql) ? sql : tableKey;

    public static string GetTableLabel(string tableKey) =>
        TableLabels.TryGetValue(tableKey, out var label) ? label : HumanizeTableName(tableKey);

    public static string GetColumnLabel(string? tableKey, string columnName)
    {
        if (ColumnLabels.TryGetValue(columnName, out var exact))
        {
            return exact;
        }

        var compositeKey = $"{tableKey}.{columnName}";
        if (!string.IsNullOrEmpty(tableKey) && ColumnLabels.TryGetValue(compositeKey, out var scoped))
        {
            return scoped;
        }

        return HumanizeColumnName(columnName);
    }

    public static IReadOnlyList<string> SortTableKeys(IEnumerable<string> discovered)
    {
        var set = new HashSet<string>(discovered, StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();

        foreach (var key in PreferredTableOrder)
        {
            if (set.Remove(key))
            {
                ordered.Add(key);
            }
        }

        ordered.AddRange(set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        return ordered;
    }

    private static string HumanizeTableName(string tableKey)
    {
        if (tableKey.StartsWith("AspNet", StringComparison.OrdinalIgnoreCase))
        {
            return "جدول " + tableKey;
        }

        var parts = tableKey.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var words = parts.Select(p => TokenLabels.TryGetValue(p, out var w) && !string.IsNullOrEmpty(w) ? w : p).Where(w => w.Length > 0);
        return "جدول " + string.Join(" ", words);
    }

    private static string HumanizeColumnName(string columnName)
    {
        if (columnName.EndsWith("Id", StringComparison.Ordinal) && columnName.Length > 2)
        {
            var stem = columnName[..^2];
            var stemLabel = TokenLabels.TryGetValue(stem, out var s) ? s : stem;
            return $"معرف {stemLabel}".Trim();
        }

        if (columnName.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
        {
            var stem = columnName[..^3];
            var stemLabel = TokenLabels.TryGetValue(stem, out var s) ? s : stem.Replace('_', ' ');
            return $"معرف {stemLabel}".Trim();
        }

        var parts = Regex.Split(columnName, @"[_\s]+|(?=[A-Z])")
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim().ToLowerInvariant());

        var translated = parts
            .Select(p => TokenLabels.TryGetValue(p, out var w) && !string.IsNullOrEmpty(w) ? w : p)
            .Where(w => w.Length > 0)
            .ToList();

        return translated.Count > 0 ? string.Join(" ", translated) : columnName;
    }
}
