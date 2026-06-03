using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace SchoolsManagement.Api.Services;

/// <summary>
/// يكتب الأخطاء في ErrorLest.txt (اسم الدالة + السطر) دون إظهارها للعميل.
/// </summary>
public sealed class ErrorLestLogger
{
    private static readonly Regex StackLineWithFile = new(
        @"^\s*at\s+(?<method>.+?)\s+in\s+(?<file>.+?):line\s+(?<line>\d+)\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex StackLineMethodOnly = new(
        @"^\s*at\s+(?<method>.+?)\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private readonly string _logFilePath;
    private readonly object _writeLock = new();

    public ErrorLestLogger(IWebHostEnvironment env)
    {
        _logFilePath = Path.Combine(env.ContentRootPath, "ErrorLest.txt");
    }

    public void Log(Exception exception, HttpContext? context = null)
    {
        if (exception == null)
        {
            return;
        }

        try
        {
            var entry = BuildEntry(exception, context);
            lock (_writeLock)
            {
                File.AppendAllText(_logFilePath, entry, Encoding.UTF8);
            }
        }
        catch
        {
            // لا نُعطّل الطلب إذا فشل الكتابة على القرص
        }
    }

    private static string BuildEntry(Exception exception, HttpContext? context)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine($"========== {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========");

        if (context != null)
        {
            sb.AppendLine($"طلب: {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                sb.AppendLine($"مستخدم: {context.User.Identity.Name}");
            }
        }

        AppendExceptionBlock(sb, exception);
        sb.AppendLine();
        return sb.ToString();
    }

    private static void AppendExceptionBlock(StringBuilder sb, Exception ex, int depth = 0)
    {
        var prefix = depth == 0 ? "" : $"  [{depth}] ";
        sb.AppendLine($"{prefix}نوع: {ex.GetType().FullName}");
        sb.AppendLine($"{prefix}رسالة: {ex.Message}");

        var frames = CollectFrames(ex).ToList();
        if (frames.Count > 0)
        {
            sb.AppendLine($"{prefix}المكدس:");
            foreach (var line in frames)
            {
                sb.AppendLine($"{prefix}  • {line}");
            }
        }
        else if (!string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            sb.AppendLine($"{prefix}المكدس (نص خام):");
            foreach (var raw in ex.StackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                sb.AppendLine($"{prefix}  {raw}");
            }
        }

        if (ex.InnerException != null)
        {
            sb.AppendLine($"{prefix}سبب داخلي:");
            AppendExceptionBlock(sb, ex.InnerException, depth + 1);
        }
    }

    private static IEnumerable<string> CollectFrames(Exception ex)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var trace = new StackTrace(ex, fNeedFileInfo: true);
        var frames = trace.GetFrames();
        if (frames is { Length: > 0 })
        {
            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                if (method == null)
                {
                    continue;
                }

                var typeName = method.DeclaringType?.FullName ?? "?";
                var methodName = method.Name;
                var file = frame.GetFileName();
                var line = frame.GetFileLineNumber();
                var label = FormatFrameLabel($"{typeName}.{methodName}", file, line);
                if (seen.Add(label))
                {
                    yield return label;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            yield break;
        }

        foreach (Match m in StackLineWithFile.Matches(ex.StackTrace))
        {
            var label = FormatFrameLabel(m.Groups["method"].Value.Trim(), m.Groups["file"].Value.Trim(), int.Parse(m.Groups["line"].Value));
            if (seen.Add(label))
            {
                yield return label;
            }
        }

        foreach (Match m in StackLineMethodOnly.Matches(ex.StackTrace))
        {
            var method = m.Groups["method"].Value.Trim();
            if (method.Contains("in ", StringComparison.Ordinal))
            {
                continue;
            }

            var label = FormatFrameLabel(method, null, 0);
            if (seen.Add(label))
            {
                yield return label;
            }
        }
    }

    private static string FormatFrameLabel(string method, string? file, int line)
    {
        if (!string.IsNullOrEmpty(file) && line > 0)
        {
            return $"{method} — {Path.GetFileName(file)} سطر {line}";
        }

        if (line > 0)
        {
            return $"{method} — سطر {line}";
        }

        return method;
    }
}
