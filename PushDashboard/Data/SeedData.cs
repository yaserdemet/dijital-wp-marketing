using Microsoft.AspNetCore.Identity;
using PushDashboard.Models;

namespace PushDashboard.Data;

public static class SeedData
{
    public static async Task Initialize(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        // Create roles if they don't exist
        string[] roleNames = { "Admin", "User", "CompanyOwner" };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Create default company if it doesn't exist
        if (!context.Companies.Any())
        {
            var defaultCompany = new Company
            {
                Name = "Default Company",
                Email = "info@defaultcompany.com",
                CreditBalance = 5000.00m, // 5000 TL başlangıç kredisi
                CreatedAt = DateTime.Now
            };

            context.Companies.Add(defaultCompany);
            await context.SaveChangesAsync();

            // Create admin user if it doesn't exist
            var adminEmail = "admin@example.com";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User",
                    CompanyId = defaultCompany.Id,
                    CreditBalance = 1000,
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    await userManager.AddToRoleAsync(adminUser, "CompanyOwner");
                }
            }

            // Create System User if it doesn't exist
            var systemUserId = "785f0c69-61ae-4218-ae43-7ea7ce299e9b";
            if (await userManager.FindByIdAsync(systemUserId) == null)
            {
                var systemUser = new ApplicationUser
                {
                    Id = systemUserId,
                    UserName = "system@pushdashboard.com",
                    Email = "system@pushdashboard.com",
                    EmailConfirmed = true,
                    FirstName = "System",
                    LastName = "User",
                    CompanyId = defaultCompany.Id, // Assign to Default Company for consistency
                    CreditBalance = 0,
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(systemUser, "SystemUser123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(systemUser, "Admin");
                }
            }

            // Create 20 test users for Default Company
            await CreateTestUsers(userManager, defaultCompany.Id);
        }
    }

    private static async Task CreateTestUsers(UserManager<ApplicationUser> userManager, Guid companyId)
    {
        var testUsers = new[]
        {
            new { FirstName = "Ahmet", LastName = "Yılmaz", Email = "ahmet.yilmaz@defaultcompany.com" },
            new { FirstName = "Mehmet", LastName = "Kaya", Email = "mehmet.kaya@defaultcompany.com" },
            new { FirstName = "Ayşe", LastName = "Demir", Email = "ayse.demir@defaultcompany.com" },
            new { FirstName = "Fatma", LastName = "Çelik", Email = "fatma.celik@defaultcompany.com" },
            new { FirstName = "Mustafa", LastName = "Şahin", Email = "mustafa.sahin@defaultcompany.com" },
            new { FirstName = "Emine", LastName = "Yıldız", Email = "emine.yildiz@defaultcompany.com" },
            new { FirstName = "Ali", LastName = "Aydın", Email = "ali.aydin@defaultcompany.com" },
            new { FirstName = "Hatice", LastName = "Özkan", Email = "hatice.ozkan@defaultcompany.com" },
            new { FirstName = "Hüseyin", LastName = "Arslan", Email = "huseyin.arslan@defaultcompany.com" },
            new { FirstName = "Zeynep", LastName = "Doğan", Email = "zeynep.dogan@defaultcompany.com" },
            new { FirstName = "İbrahim", LastName = "Aslan", Email = "ibrahim.aslan@defaultcompany.com" },
            new { FirstName = "Elif", LastName = "Polat", Email = "elif.polat@defaultcompany.com" },
            new { FirstName = "Ömer", LastName = "Koç", Email = "omer.koc@defaultcompany.com" },
            new { FirstName = "Merve", LastName = "Güneş", Email = "merve.gunes@defaultcompany.com" },
            new { FirstName = "Burak", LastName = "Erdoğan", Email = "burak.erdogan@defaultcompany.com" },
            new { FirstName = "Seda", LastName = "Yavuz", Email = "seda.yavuz@defaultcompany.com" },
            new { FirstName = "Emre", LastName = "Özdemir", Email = "emre.ozdemir@defaultcompany.com" },
            new { FirstName = "Gizem", LastName = "Kılıç", Email = "gizem.kilic@defaultcompany.com" },
            new { FirstName = "Serkan", LastName = "Aksoy", Email = "serkan.aksoy@defaultcompany.com" },
            new { FirstName = "Deniz", LastName = "Tunç", Email = "deniz.tunc@defaultcompany.com" }
        };

        foreach (var userData in testUsers)
        {
            // Check if user already exists
            if (await userManager.FindByEmailAsync(userData.Email) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = userData.Email,
                    Email = userData.Email,
                    EmailConfirmed = true,
                    FirstName = userData.FirstName,
                    LastName = userData.LastName,
                    CompanyId = companyId,
                    CreatedAt = DateTime.Now.AddDays(-Random.Shared.Next(1, 365)), // Random creation date within last year
                    LastLoginAt = DateTime.Now.AddDays(-Random.Shared.Next(1, 30)) // Random last login within last month
                };

                var result = await userManager.CreateAsync(user, "User123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "User");
                }
            }
        }
    }
}
