
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagementSystem.Controllers;

public class UserListItem
{
    public ApplicationUser User { get; set; } = null!;
    public IList<string> Roles { get; set; } = new List<string>();
}

[Authorize(Roles = StaffRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.OrderBy(u => u.FullName).ToList();
        var rows = new List<UserListItem>();
        foreach (var user in users)
        {
            rows.Add(new UserListItem
            {
                User = user,
                Roles = await _userManager.GetRolesAsync(user)
            });
        }
        return View(rows);
    }

    public IActionResult Create()
    {
        ViewBag.Roles = StaffRoles.All;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string fullName, string userName, string? email, string password, string role)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(userName)
            || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
        {
            ModelState.AddModelError("", "Full name, username, password and role are required.");
            ViewBag.Roles = StaffRoles.All;
            return View();
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            FullName = fullName,
            EmailConfirmed = !string.IsNullOrWhiteSpace(email),
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            ViewBag.Roles = StaffRoles.All;
            return View();
        }

        await _userManager.AddToRoleAsync(user, role);

        TempData["Success"] = $"{fullName} created with role \"{role}\". They can now log in at /Identity/Account/Login.";
        return RedirectToAction(nameof(Index));
    }

    // Deactivating locks the account out indefinitely instead of deleting it,
    // so their historical records (consultations, invoices, etc.) stay intact.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.IsActive = !user.IsActive;

        if (!user.IsActive)
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }

        await _userManager.UpdateAsync(user);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        ViewBag.Roles = StaffRoles.All;
        ViewBag.CurrentRole = currentRoles.FirstOrDefault();
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, string fullName, string userName, string? email, string role, string? newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.FullName = fullName;
        user.UserName = userName;
        user.Email = string.IsNullOrWhiteSpace(email) ? null : email;
        await _userManager.UpdateAsync(user);

        // Simplification: each user has exactly one role, so swap it wholesale
        // rather than trying to diff added/removed roles.
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!string.IsNullOrWhiteSpace(role))
            await _userManager.AddToRoleAsync(user, role);

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                ViewBag.Roles = StaffRoles.All;
                ViewBag.CurrentRole = role;
                return View(user);
            }
        }

        TempData["Success"] = $"{fullName} updated.";
        return RedirectToAction(nameof(Index));
    }
}







//using HospitalManagementSystem.Data;
//using HospitalManagementSystem.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;

//namespace HospitalManagementSystem.Controllers;

//public class UserListItem
//{
//    public ApplicationUser User { get; set; } = null!;
//    public IList<string> Roles { get; set; } = new List<string>();
//}

//[Authorize(Roles = StaffRoles.Admin)]
//public class UsersController : Controller
//{
//    private readonly UserManager<ApplicationUser> _userManager;

//    public UsersController(UserManager<ApplicationUser> userManager)
//    {
//        _userManager = userManager;
//    }

//    public async Task<IActionResult> Index()
//    {
//        var users = _userManager.Users.OrderBy(u => u.FullName).ToList();
//        var rows = new List<UserListItem>();
//        foreach (var user in users)
//        {
//            rows.Add(new UserListItem
//            {
//                User = user,
//                Roles = await _userManager.GetRolesAsync(user)
//            });
//        }
//        return View(rows);
//    }

//    public IActionResult Create()
//    {
//        ViewBag.Roles = StaffRoles.All;
//        return View();
//    }

//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> Create(string fullName, string email, string password, string role)
//    {
//        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email)
//            || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
//        {
//            ModelState.AddModelError("", "All fields are required.");
//            ViewBag.Roles = StaffRoles.All;
//            return View();
//        }

//        var user = new ApplicationUser
//        {
//            UserName = email,
//            Email = email,
//            FullName = fullName,
//            EmailConfirmed = true,
//            IsActive = true
//        };

//        var result = await _userManager.CreateAsync(user, password);
//        if (!result.Succeeded)
//        {
//            foreach (var error in result.Errors)
//                ModelState.AddModelError("", error.Description);
//            ViewBag.Roles = StaffRoles.All;
//            return View();
//        }

//        await _userManager.AddToRoleAsync(user, role);

//        TempData["Success"] = $"{fullName} created with role \"{role}\". They can now log in at /Identity/Account/Login.";
//        return RedirectToAction(nameof(Index));
//    }

//    // Deactivating locks the account out indefinitely instead of deleting it,
//    // so their historical records (consultations, invoices, etc.) stay intact.
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> ToggleActive(string id)
//    {
//        var user = await _userManager.FindByIdAsync(id);
//        if (user is null) return NotFound();

//        user.IsActive = !user.IsActive;

//        if (!user.IsActive)
//        {
//            await _userManager.SetLockoutEnabledAsync(user, true);
//            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
//        }
//        else
//        {
//            await _userManager.SetLockoutEndDateAsync(user, null);
//        }

//        await _userManager.UpdateAsync(user);
//        return RedirectToAction(nameof(Index));
//    }

//    public async Task<IActionResult> Edit(string id)
//    {
//        var user = await _userManager.FindByIdAsync(id);
//        if (user is null) return NotFound();

//        var currentRoles = await _userManager.GetRolesAsync(user);
//        ViewBag.Roles = StaffRoles.All;
//        ViewBag.CurrentRole = currentRoles.FirstOrDefault();
//        return View(user);
//    }

//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> Edit(string id, string fullName, string email, string role, string? newPassword)
//    {
//        var user = await _userManager.FindByIdAsync(id);
//        if (user is null) return NotFound();

//        user.FullName = fullName;
//        user.Email = email;
//        user.UserName = email;
//        await _userManager.UpdateAsync(user);

//        // Simplification: each user has exactly one role, so swap it wholesale
//        // rather than trying to diff added/removed roles.
//        var currentRoles = await _userManager.GetRolesAsync(user);
//        if (currentRoles.Count > 0)
//            await _userManager.RemoveFromRolesAsync(user, currentRoles);
//        if (!string.IsNullOrWhiteSpace(role))
//            await _userManager.AddToRoleAsync(user, role);

//        if (!string.IsNullOrWhiteSpace(newPassword))
//        {
//            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
//            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
//            if (!result.Succeeded)
//            {
//                foreach (var error in result.Errors)
//                    ModelState.AddModelError("", error.Description);
//                ViewBag.Roles = StaffRoles.All;
//                ViewBag.CurrentRole = role;
//                return View(user);
//            }
//        }

//        TempData["Success"] = $"{fullName} updated.";
//        return RedirectToAction(nameof(Index));
//    }
//}