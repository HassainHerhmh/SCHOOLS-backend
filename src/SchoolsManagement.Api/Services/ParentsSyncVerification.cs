using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Services;

public static class ParentsSyncVerification
{
    public static ParentsPublishOutcome Evaluate(
        ParentsRemoteSyncPublisher.ParentsSyncPlan plan,
        ParentsIngestResult uploaded,
        ParentsRemoteDataCounts? remote)
    {
        var issues = new List<string>();

        if (plan.HasChanges && uploaded.Total <= 0)
        {
            issues.Add("استجابة السيرفر الخارجي لم تُسجّل أي سجل محفوظ — تحقق من مفتاح المزامنة X-Parents-Sync-Key.");
        }

        if (plan.SyncStudents && plan.ChangedStudents > 0)
        {
            if (uploaded.Students <= 0)
            {
                issues.Add($"لم يُحفظ أي طالب من أصل {plan.ChangedStudents}.");
            }
            else if (remote is null)
            {
                issues.Add("تعذر قراءة عدد الطلاب من السيرفر الخارجي بعد الرفع.");
            }
            else if (remote.Students <= 0)
            {
                issues.Add(
                    $"جدول الطلاب على السيرفر الخارجي فارغ رغم رفع {uploaded.Students} سجلًا — تحقق من قاعدة MySQL وجداول parents_students_summary.");
            }
        }

        if (plan.SyncClasses && plan.ChangedClasses > 0)
        {
            if (uploaded.Classes <= 0)
            {
                issues.Add($"لم يُحفظ أي صف من أصل {plan.ChangedClasses}.");
            }
            else if (remote?.Classes <= 0)
            {
                issues.Add("جدول الصفوف على السيرفر الخارجي فارغ بعد الرفع.");
            }
        }

        if (plan.SyncSections && plan.ChangedSections > 0)
        {
            if (uploaded.Sections <= 0)
            {
                issues.Add($"لم تُحفظ أي شعبة من أصل {plan.ChangedSections}.");
            }
            else if (remote?.Sections <= 0)
            {
                issues.Add("جدول الشعب على السيرفر الخارجي فارغ بعد الرفع.");
            }
        }

        if (plan.SyncAttendance && plan.ChangedAttendance > 0)
        {
            if (uploaded.Attendance <= 0)
            {
                issues.Add($"لم يُحفظ أي سجل حضور من أصل {plan.ChangedAttendance}.");
            }
            else if (remote?.Attendance <= 0)
            {
                issues.Add("جدول الحضور على السيرفر الخارجي فارغ بعد الرفع.");
            }
        }

        if (plan.SyncStudentReports && plan.ChangedStudentReports > 0)
        {
            if (uploaded.StudentReports <= 0)
            {
                issues.Add($"لم يُحفظ أي تقرير طالب من أصل {plan.ChangedStudentReports}.");
            }
            else if (remote?.StudentReports <= 0)
            {
                issues.Add("جدول تقارير الطلاب على السيرفر الخارجي فارغ بعد الرفع.");
            }
        }

        if (plan.SyncInstallments && plan.ChangedInstallments > 0)
        {
            if (uploaded.Installments <= 0)
            {
                issues.Add($"لم تُحفظ أقساط الطلاب من أصل {plan.ChangedInstallments} طالب.");
            }
            else if (remote?.Installments <= 0)
            {
                issues.Add("جدول أقساط الطلاب على السيرفر الخارجي فارغ بعد الرفع.");
            }
        }

        if (plan.SyncSchedule && plan.ChangedSchedule > 0)
        {
            if (uploaded.SchedulePeriods <= 0 && uploaded.ScheduleSettings <= 0)
            {
                issues.Add($"لم يُحفظ جدول الحصص من أصل {plan.ChangedSchedule} حصة.");
            }
            else if (remote?.SchedulePeriods <= 0)
            {
                issues.Add("جدول الحصص على السيرفر الخارجي فارغ بعد الرفع.");
            }
        }

        if (uploaded.Grades > 0)
        {
            if (remote is null)
            {
                issues.Add("تعذر قراءة عدد الدرجات من السيرفر الخارجي بعد الرفع.");
            }
            else if (remote.Grades <= 0)
            {
                issues.Add(
                    $"جدول الدرجات على السيرفر الخارجي فارغ رغم رفع {uploaded.Grades} سجلًا — تحقق من جدول parents_grades.");
            }
        }

        if (uploaded.Subjects > 0 && remote?.Subjects <= 0)
        {
            issues.Add("جدول المواد على السيرفر الخارجي فارغ بعد الرفع.");
        }

        if (uploaded.Exams > 0 && remote?.Exams <= 0)
        {
            issues.Add("جدول الاختبارات على السيرفر الخارجي فارغ بعد الرفع.");
        }

        if (issues.Count > 0)
        {
            return new ParentsPublishOutcome
            {
                Success = false,
                Message = "فشل التحقق: البيانات لم تصل بشكل صحيح إلى السيرفر الخارجي.",
                FailureReason = string.Join(" ", issues),
                Uploaded = uploaded,
                Remote = remote
            };
        }

        var parts = new List<string>();
        if (uploaded.Students > 0)
        {
            parts.Add($"{uploaded.Students} طالب");
        }

        if (uploaded.Classes > 0)
        {
            parts.Add($"{uploaded.Classes} صف");
        }

        if (uploaded.Sections > 0)
        {
            parts.Add($"{uploaded.Sections} شعبة");
        }

        if (uploaded.Attendance > 0)
        {
            parts.Add($"{uploaded.Attendance} حضور");
        }

        if (uploaded.StudentReports > 0)
        {
            parts.Add($"{uploaded.StudentReports} تقرير");
        }

        if (uploaded.Installments > 0)
        {
            parts.Add($"{uploaded.Installments} قسط");
        }

        if (uploaded.SchedulePeriods > 0)
        {
            parts.Add($"{uploaded.SchedulePeriods} حصة");
        }

        if (uploaded.Grades > 0)
        {
            parts.Add($"{uploaded.Grades} درجة");
        }

        if (uploaded.Subjects > 0)
        {
            parts.Add($"{uploaded.Subjects} مادة");
        }

        if (uploaded.Exams > 0)
        {
            parts.Add($"{uploaded.Exams} اختبار");
        }

        var detail = parts.Count > 0 ? string.Join("، ", parts) : $"{plan.TotalItems} عنصر";
        return new ParentsPublishOutcome
        {
            Success = true,
            Message = $"تم تحديث بيانات تطبيق أولياء الأمور على السيرفر الخارجي بنجاح ({detail}).",
            Uploaded = uploaded,
            Remote = remote
        };
    }
}


