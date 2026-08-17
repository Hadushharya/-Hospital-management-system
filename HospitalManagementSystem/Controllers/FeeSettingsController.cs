using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

// Admin-only: manage the fee schedule (payment order id, name, amount) that
// Reception/OPD pick from instead of typing free-text amounts.
[Authorize(Roles = StaffRoles.Admin)]
public class FeeSettingsController : Controller
{
    private readonly ApplicationDbContext _db;
    public FeeSettingsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var settings = await _db.FeeSettings.OrderBy(f => f.FeeType).ThenBy(f => f.PaymentName).ToListAsync();
        return View(settings);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FeeSetting setting)
    {
        if (!ModelState.IsValid) return View(setting);
        _db.FeeSettings.Add(setting);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{setting.PaymentName}\" added — payment order #{setting.Id}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var setting = await _db.FeeSettings.FindAsync(id);
        if (setting is null) return NotFound();
        setting.IsActive = !setting.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
