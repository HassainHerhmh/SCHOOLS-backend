using System.Linq;
using Microsoft.AspNetCore.Identity;
using SchoolsManagement.Api.Models.Identity;

namespace SchoolsManagement.Api.Services;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger logger,
        CancellationToken ct = default)
    {
        string[] roles = { "Admin", "Teacher", "Staff" };
        foreach (var roleName in roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var r = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!r.Succeeded)
            {
                logger.LogWarning("تعذّر إنشاء الدور {Role}: {Errors}", roleName,
                    string.Join("; ", r.Errors.Select(e => e.Description)));
            }
        }

        const string seedUserName = "mansour.admin";
        if (await userManager.FindByNameAsync(seedUserName) != null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = seedUserName,
            Email = "mansour@school.local",
            FullName = "منصور الديح",
            PhoneNumber = "0500000000",
            UserType = "إداري",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
        };

        const string seedPassword = "Mansour@Admin1";
        var create = await userManager.CreateAsync(user, seedPassword);
        if (!create.Succeeded)
        {
            logger.LogWarning("تعذّر إنشاء المستخدم الافتراضي منصور: {Errors}",
                string.Join("; ", create.Errors.Select(e => e.Description)));
            return;
        }

        var roleAdd = await userManager.AddToRoleAsync(user, "Admin");
        if (!roleAdd.Succeeded)
        {
            logger.LogWarning("تعذّر ربط منصور بدور Admin: {Errors}",
                string.Join("; ", roleAdd.Errors.Select(e => e.Description)));
        }
        else
        {
            logger.LogInformation("تم إنشاء مستخدم افتراضي: {User} / كلمة المرور: {Pwd}", seedUserName, seedPassword);
        }
    }
}
