using System.Text.Json;
using SchoolsManagement.Api.Security;

namespace SchoolsManagement.Api.Middleware;

/// <summary>
/// عند إرسال JWT: يفرض صلاحيات الصفحات على مسارات الـ API.
/// مسارات عامة (تسجيل الدخول) وطلبات بلا رمز تمر كما هي.
/// </summary>
public class ApiPermissionMiddleware
{
    private readonly RequestDelegate _next;

    public ApiPermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            || ApiPermissionMap.IsPublicApiPath(path))
        {
            await _next(context);
            return;
        }

        var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
        var requiresAuth = ApiPermissionMap.RequiresAuthentication(path);
        var requiredPermission = ApiPermissionMap.GetRequiredPermission(path);

        if (requiresAuth && !isAuthenticated)
        {
            await WriteJsonAsync(context, StatusCodes.Status401Unauthorized, "يلزم تسجيل الدخول.");
            return;
        }

        if (!isAuthenticated)
        {
            await _next(context);
            return;
        }

        var permissionClaims = context.User.Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToList();
        if (permissionClaims.Count >= PermissionCatalog.AllKeys.Count)
        {
            await _next(context);
            return;
        }

        if (requiredPermission is null)
        {
            await _next(context);
            return;
        }

        var hasPermission = context.User.Claims
            .Any(c => c.Type == "permission" && string.Equals(c.Value, requiredPermission, StringComparison.OrdinalIgnoreCase));

        if (!hasPermission)
        {
            await WriteJsonAsync(context, StatusCodes.Status403Forbidden, "ليس لديك صلاحية لهذه العملية.");
            return;
        }

        await _next(context);
    }

    private static async Task WriteJsonAsync(HttpContext context, int status, string message)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = JsonSerializer.Serialize(new { message }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        });
        await context.Response.WriteAsync(payload);
    }
}
