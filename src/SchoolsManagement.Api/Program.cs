using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolsManagement.Api.Configuration;
using SchoolsManagement.Api.Data;
using SchoolsManagement.Api.Models.Identity;
using SchoolsManagement.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

// Railway / Docker: المنفذ من متغير PORT (مثلاً 8080)
var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(railwayPort))
{
    builder.WebHost.UseUrls($"http://+:{railwayPort}");
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var sqlConnectionString = ConnectionStringResolver.TryResolve(builder.Configuration);
var databaseConfigured = !string.IsNullOrWhiteSpace(sqlConnectionString);

if (!databaseConfigured && ConnectionStringResolver.IsRailwayHost())
{
    builder.Logging.AddConsole();
    var msg = ConnectionStringResolver.BuildMissingConnectionMessage();
    Console.WriteLine("[SchoolsManagement.Api] " + msg.ReplaceLineEndings(" | "));
    sqlConnectionString = ConnectionStringResolver.PlaceholderConnectionString;
}
else if (!databaseConfigured)
{
    sqlConnectionString = ConnectionStringResolver.Resolve(builder.Configuration);
}
else if (ConnectionStringResolver.IsRailwayHost()
         && ConnectionStringResolver.LooksLikeLocalSql(sqlConnectionString))
{
    throw new InvalidOperationException(
        """
        Railway: لا يمكن استخدام localhost\SQLEXPRESS على السحابة.
        عيّن ConnectionStrings__DefaultConnection لـ Azure SQL أو SQL Server سحابي.
        """);
}
else if (ConnectionStringResolver.LooksLikeMySql(sqlConnectionString))
{
    throw new InvalidOperationException(
        "سلسلة الاتصال MySQL — هذا API يحتاج SQL Server. عيّن ConnectionStrings__DefaultConnection لـ Azure SQL.");
}

builder.Services.AddSingleton(new DatabaseConfigState(databaseConfigured, sqlConnectionString!));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 4;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is missing.");
var jwtIssuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is missing.");
var jwtAudience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT Audience is missing.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<UserPermissionService>();
builder.Services.AddScoped<PermissionMatrixService>();
builder.Services.AddScoped<ParentsAppIngestService>();
builder.Services.AddScoped<ParentsRemoteSyncPublisher>();
builder.Services.AddSingleton<DatabaseHealthChecker>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<SalaryJournalMonthEndHostedService>();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
    options.JsonSerializerOptions.AllowTrailingCommas = true;
});
builder.Services.AddEndpointsApiExplorer();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("LanPolicy", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin))
                {
                    return false;
                }

                Uri? uri = null;
                try
                {
                    uri = new Uri(origin);
                }
                catch
                {
                    return false;
                }

                static bool IsLanIp(IPAddress addr)
                {
                    if (IPAddress.IsLoopback(addr))
                    {
                        return true;
                    }

                    if (addr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return false;
                    }

                    var b = addr.GetAddressBytes();
                    if (b[0] == 10)
                    {
                        return true;
                    }

                    if (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                    {
                        return true;
                    }

                    if (b[0] == 192 && b[1] == 168)
                    {
                        return true;
                    }

                    return false;
                }

                // localhost / 127.0.0.1 سواء كان الأصل http أو https (مهم لخدمة Angular الافتراضية).
                var hostForIp = uri.Host;
                if (hostForIp.Length >= 2 && hostForIp[0] == '[' && hostForIp[^1] == ']')
                {
                    hostForIp = hostForIp.Substring(1, hostForIp.Length - 2);
                }

                if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (IPAddress.TryParse(hostForIp, out var loopOrLan) && (IPAddress.IsLoopback(loopOrLan) || IsLanIp(loopOrLan)))
                {
                    return true;
                }

                return false;
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// استثناءات غير المعالجة → JSON بصيغة snake_case لتقرأها الواجهة (رسالة واضحة بدل صفحة HTML)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var handler = context.Features.Get<IExceptionHandlerFeature>();
        var ex = handler?.Error;
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";

        var opts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var payload = new Dictionary<string, string?>
        {
            ["message"] = string.IsNullOrWhiteSpace(ex?.Message)
                ? "خطأ في الخادم."
                : ex!.Message
        };

        if (app.Environment.IsDevelopment() && ex != null)
        {
            payload["detail"] = ex.ToString();
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, opts));
    });
});

// تطبيق هجرات EF تلقائياً (إنشاء journal_types / payment_types / receipt_types وغيرها عند غيابها في SchoolsDb)
var dbConfig = app.Services.GetRequiredService<DatabaseConfigState>();
if (!dbConfig.IsConfigured)
{
    app.Logger.LogWarning(
        "قاعدة البيانات غير مضبوطة — افتح /api/health/setup وأضف ConnectionStrings__DefaultConnection على Railway.");
}
else
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "فشل تطبيق هجرات قاعدة البيانات. تأكد من Connection String وتشغيل SQL Server.");
    }

    try
    {
        EmployeePayrollSchemaBootstrap.EnsureEmployeeChartAccountColumn(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "فشل التحقق من عمود chart_account_id في جدول الموظفين.");
    }

    try
    {
        AccountingVoucherTablesBootstrap.EnsureExists(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "فشل إنشاء/التحقق من جداول سندات القبض والصرف والقيود اليومية ومصارفة العملة.");
    }

    try
    {
        JournalEntryPostedAtBootstrap.EnsurePostedAtColumn(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "فشل إضافة عمود posted_at لجدول القيود اليومية.");
    }

    try
    {
        UserPagePermissionsBootstrap.EnsureTable(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "فشل إنشاء جدول صلاحيات الصفحات.");
    }

    try
    {
        ApplicationUserPermissionsJsonBootstrap.EnsureColumn(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "فشل إضافة عمود permissions_json لجدول المستخدمين.");
    }

    try
    {
        SchoolExtendedTablesBootstrap.EnsureExists(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "فشل إنشاء جداول المدفوعات والخصومات والدرجات والمواد.");
    }

    try
    {
        ParentsAppTablesBootstrap.EnsureExists(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "فشل إنشاء جداول نشر بيانات تطبيق أولياء الأمور.");
    }

    try
    {
        IdentityDataSeeder.SeedAsync(
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
                scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(),
                scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeed"))
            .GetAwaiter()
            .GetResult();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "فشل تهيئة المستخدمين والأدوار الافتراضية.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // تسليم العميل / الشبكة المحلية — لا تعرض وثائق Swagger أبداً
    app.Use(async (context, next) =>
    {
        var p = context.Request.Path.Value ?? "";
        if (p.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        await next();
    });
}

// يمنع قطع الوصول من الأجهزة الأخرى عبر الشبكة (HTTP فقط غالبًا)
var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var serveSpa = Directory.Exists(wwwrootPath);
if (serveSpa)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}
else if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("LanPolicy");
app.UseAuthentication();
app.UseMiddleware<SchoolsManagement.Api.Middleware.ApiPermissionMiddleware>();
app.UseAuthorization();
app.MapControllers();

if (serveSpa)
{
    app.MapFallbackToFile("index.html");
}
else if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.Run();
