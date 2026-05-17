using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SchoolsManagement.Api.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    [StringLength(64)]
    public string UserType { get; set; } = "إداري";
}
