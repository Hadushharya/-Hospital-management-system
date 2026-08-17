using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Data;

public static class DbInitializer
{
    // Called once from Program.cs on startup. Idempotent — safe to run every launch.
    public static async Task SeedRolesAndAdminAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        foreach (var role in StaffRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Default admin account — login by username. CHANGE THIS PASSWORD before deploying anywhere real.
        const string adminUserName = "admin";
        const string adminEmail = "admin@hms.local";
        const string adminPassword = "Admin@123"; // requires RequireUppercase = false in Program.cs, otherwise use "Admin@123"

        var admin = await userManager.FindByNameAsync(adminUserName);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminUserName,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, StaffRoles.Admin);
            }
        }

        // Starter fee schedule so Reception/OPD aren't blocked with an empty list.
        // Admin can edit/deactivate these later under Fee settings.
        if (!await db.FeeSettings.AnyAsync())
        {
            db.FeeSettings.AddRange(
                new FeeSetting { PaymentName = "Registration Card Fee", FeeType = FeeType.Registration, Amount = 50, IsActive = true },
                new FeeSetting { PaymentName = "OPD Consultation Fee", FeeType = FeeType.Consultation, Amount = 100, IsActive = true },
                new FeeSetting { PaymentName = "General Lab Test Fee", FeeType = FeeType.Lab, Amount = 150, IsActive = true }
            );
            await db.SaveChangesAsync();
        }
    }
}



//using HospitalManagementSystem.Models;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;

//namespace HospitalManagementSystem.Data;

//public static class DbInitializer
//{
//    // Called once from Program.cs on startup. Idempotent — safe to run every launch.
//    public static async Task SeedRolesAndAdminAsync(IServiceProvider services)
//    {
//        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
//        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
//        var db = services.GetRequiredService<ApplicationDbContext>();

//        foreach (var role in StaffRoles.All)
//        {
//            if (!await roleManager.RoleExistsAsync(role))
//            {
//                await roleManager.CreateAsync(new IdentityRole(role));
//            }
//        }

//        // Default admin account — CHANGE THIS PASSWORD before deploying anywhere real.
//        const string adminEmail = "admin@hms.local";
//        const string adminPassword = "ChangeMe!123";

//        var admin = await userManager.FindByEmailAsync(adminEmail);
//        if (admin is null)
//        {
//            admin = new ApplicationUser
//            {
//                UserName = adminEmail,
//                Email = adminEmail,
//                FullName = "System Administrator",
//                EmailConfirmed = true
//            };

//            var result = await userManager.CreateAsync(admin, adminPassword);
//            if (result.Succeeded)
//            {
//                await userManager.AddToRoleAsync(admin, StaffRoles.Admin);
//            }
//        }

//        // Starter fee schedule so Reception/OPD aren't blocked with an empty list.
//        // Admin can edit/deactivate these later under Fee settings.
//        if (!await db.FeeSettings.AnyAsync())
//        {
//            db.FeeSettings.AddRange(
//                new FeeSetting { PaymentName = "Registration Card Fee", FeeType = FeeType.Registration, Amount = 50, IsActive = true },
//                new FeeSetting { PaymentName = "OPD Consultation Fee", FeeType = FeeType.Consultation, Amount = 100, IsActive = true },
//                new FeeSetting { PaymentName = "General Lab Test Fee", FeeType = FeeType.Lab, Amount = 150, IsActive = true }
//            );
//            await db.SaveChangesAsync();
//        }
//    }
//}
