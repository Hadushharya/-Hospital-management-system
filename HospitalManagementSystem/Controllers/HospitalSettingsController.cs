using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

[Authorize(Roles = StaffRoles.Admin)]
public class HospitalSettingsController : Controller
{
    private readonly ApplicationDbContext _db;
    public HospitalSettingsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var settings = await _db.HospitalSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new HospitalSettings();
            _db.HospitalSettings.Add(settings);
            await _db.SaveChangesAsync();
        }
        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(HospitalSettings model)
    {
        var settings = await _db.HospitalSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new HospitalSettings();
            _db.HospitalSettings.Add(settings);
        }
        settings.Name = model.Name;
        settings.Address = model.Address;
        settings.Phone = model.Phone;
        settings.TinNumber = model.TinNumber;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Hospital settings updated.";
        return RedirectToAction(nameof(Index));
    }
}