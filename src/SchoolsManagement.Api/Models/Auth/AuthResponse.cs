namespace SchoolsManagement.Api.Models.Auth;



public class AuthResponse

{

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>اسم ظاهر للهيدر — يفضّل الاسم الكامل ثم اسم مستخدم غير بريدي ثم الجوال ثم البريد.</summary>

    public string FullName { get; set; } = string.Empty;

    /// <summary>نوع المستخدم (إداري، معلم، …) من نموذج المستخدم.</summary>

    public string UserType { get; set; } = string.Empty;

    /// <summary>أول دور Identity (Admin، Teacher، Staff).</summary>

    public string Role { get; set; } = string.Empty;

    public IList<string> Permissions { get; set; } = [];

    public bool IsAdmin { get; set; }
}

