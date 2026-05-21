using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Models.Accounting;
using SchoolsManagement.Api.Models.Identity;
using SchoolsManagement.Api.Models.School;

namespace SchoolsManagement.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<GradeClass> GradeClasses => Set<GradeClass>();
    public DbSet<SchoolSection> SchoolSections => Set<SchoolSection>();
    public DbSet<StudentRecord> StudentRecords => Set<StudentRecord>();
    public DbSet<AccountGroupRecord> AccountGroups => Set<AccountGroupRecord>();
    public DbSet<ChartAccountRecord> ChartAccounts => Set<ChartAccountRecord>();
    public DbSet<CurrencyRecord> Currencies => Set<CurrencyRecord>();
    public DbSet<JournalTypeRecord> JournalTypes => Set<JournalTypeRecord>();
    public DbSet<PaymentTypeRecord> PaymentTypes => Set<PaymentTypeRecord>();
    public DbSet<ReceiptTypeRecord> ReceiptTypes => Set<ReceiptTypeRecord>();
    public DbSet<CashBoxGroupRecord> CashBoxGroups => Set<CashBoxGroupRecord>();
    public DbSet<CashBoxRecord> CashBoxes => Set<CashBoxRecord>();
    public DbSet<BankGroupRecord> BankGroups => Set<BankGroupRecord>();
    public DbSet<BankRecord> Banks => Set<BankRecord>();
    public DbSet<TransitAccountsSettingsRecord> TransitAccountsSettings => Set<TransitAccountsSettingsRecord>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<SyncCheckpointRecord> SyncCheckpoints => Set<SyncCheckpointRecord>();
    public DbSet<EmployeeRecord> EmployeeRecords => Set<EmployeeRecord>();
    public DbSet<EmployeeMonthlyAccountRecord> EmployeeMonthlyAccounts => Set<EmployeeMonthlyAccountRecord>();
    public DbSet<EmployeeAbsenceSettingRecord> EmployeeAbsenceSettings => Set<EmployeeAbsenceSettingRecord>();
    public DbSet<EmployeeMonthlyProcessRecord> EmployeeMonthlyProcesses => Set<EmployeeMonthlyProcessRecord>();
    public DbSet<BusPortalUserRecord> BusPortalUsers => Set<BusPortalUserRecord>();
    public DbSet<BusSiteRecord> BusSites => Set<BusSiteRecord>();
    public DbSet<ReceiptVoucherRecord> ReceiptVouchers => Set<ReceiptVoucherRecord>();
    public DbSet<PaymentVoucherRecord> PaymentVouchers => Set<PaymentVoucherRecord>();
    public DbSet<VoucherJournalEntryRecord> VoucherJournalEntries => Set<VoucherJournalEntryRecord>();
    public DbSet<CurrencyExchangeRecord> CurrencyExchanges => Set<CurrencyExchangeRecord>();
    public DbSet<UserPagePermissionRecord> UserPagePermissions => Set<UserPagePermissionRecord>();
    public DbSet<StudentPaymentRecord> StudentPayments => Set<StudentPaymentRecord>();
    public DbSet<TransferApprovalRequestRecord> TransferApprovalRequests => Set<TransferApprovalRequestRecord>();
    public DbSet<StudentDiscountRecord> StudentDiscounts => Set<StudentDiscountRecord>();
    public DbSet<StudentDiscountApplicationRecord> StudentDiscountApplications => Set<StudentDiscountApplicationRecord>();
    public DbSet<SubjectRecord> Subjects => Set<SubjectRecord>();
    public DbSet<ExamRecord> Exams => Set<ExamRecord>();
    public DbSet<GradeRuleRecord> GradeRules => Set<GradeRuleRecord>();
    public DbSet<GradeRecord> Grades => Set<GradeRecord>();
    public DbSet<ParentsStudentSummaryRecord> ParentsStudentSummaries => Set<ParentsStudentSummaryRecord>();
    public DbSet<ParentsClassPublishRecord> ParentsClassPublishes => Set<ParentsClassPublishRecord>();
    public DbSet<ParentsSectionPublishRecord> ParentsSectionPublishes => Set<ParentsSectionPublishRecord>();
    public DbSet<ParentsAttendanceSummaryRecord> ParentsAttendanceSummaries => Set<ParentsAttendanceSummaryRecord>();
    public DbSet<ParentsStudentReportRecord> ParentsStudentReports => Set<ParentsStudentReportRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<GradeClass>()
            .HasMany(g => g.Sections)
            .WithOne(s => s.Class)
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ChartAccountRecord>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => x.Id).IsUnique();
        });

        builder.Entity<EmployeeRecord>(e =>
        {
            e.Property(x => x.ChartAccountId).HasColumnName("chart_account_id");
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.ChartAccountId)
                .IsUnique()
                .HasFilter("[chart_account_id] IS NOT NULL");
        });

        builder.Entity<EmployeeMonthlyAccountRecord>(e =>
        {
            e.HasIndex(x => new { x.EmployeeId, x.Year, x.Month }).IsUnique();
        });

        builder.Entity<EmployeeAbsenceSettingRecord>(e =>
        {
            e.HasIndex(x => new { x.Year, x.Month }).IsUnique();
        });

        builder.Entity<EmployeeMonthlyProcessRecord>(e =>
        {
            e.HasIndex(x => new { x.Year, x.Month }).IsUnique();
        });

        builder.Entity<BusPortalUserRecord>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
        });

        builder.Entity<BusSiteRecord>(e =>
        {
            e.HasIndex(x => x.SiteName).IsUnique();
        });

        builder.Entity<ReceiptVoucherRecord>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
        });

        builder.Entity<PaymentVoucherRecord>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
        });

        builder.Entity<VoucherJournalEntryRecord>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            var referenceIndex = e.HasIndex(x => x.Reference)
                .IsUnique()
                .HasDatabaseName("IX_journal_entries_reference");
            if (Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true)
            {
                referenceIndex.HasFilter("`reference` <> ''");
            }
            else
            {
                referenceIndex.HasFilter("[reference] <> N''");
            }
        });

        builder.Entity<CurrencyExchangeRecord>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
        });

        builder.Entity<UserPagePermissionRecord>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.PermissionKey }).IsUnique();
            e.HasIndex(x => x.UserId);
        });

        builder.Entity<GradeRuleRecord>(e =>
        {
            e.HasIndex(x => new { x.ClassId, x.SubjectId }).IsUnique();
        });

        builder.Entity<ParentsAttendanceSummaryRecord>(e =>
        {
            e.HasKey(x => new { x.StudentId, x.Date });
        });
    }
}
