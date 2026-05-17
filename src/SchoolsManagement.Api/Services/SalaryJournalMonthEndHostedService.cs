using SchoolsManagement.Api.Data;

namespace SchoolsManagement.Api.Services;

/// <summary>يُرحِّل قيود الرواتب المعلقة تلقائياً في آخر يوم من كل شهر.</summary>
public sealed class SalaryJournalMonthEndHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SalaryJournalMonthEndHostedService> _logger;

    public SalaryJournalMonthEndHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SalaryJournalMonthEndHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (SalaryJournalPostingService.IsLastDayOfMonth(DateTime.UtcNow.Date))
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var n = await SalaryJournalPostingService.TryAutoPostAtMonthEndAsync(db, stoppingToken);
                    if (n > 0)
                    {
                        _logger.LogInformation("تم ترحيل {Count} قيد راتب تلقائياً (نهاية الشهر).", n);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "فشل التحقق من ترحيل رواتب نهاية الشهر.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
