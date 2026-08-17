using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

[Authorize(Roles = StaffRoles.TriageNurse + "," + StaffRoles.Admin)]
public class TriageController : Controller
{
    private readonly ApplicationDbContext _db;
    public TriageController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var pending = await _db.Visits
            .Include(v => v.Patient)
            .Where(v => v.Triage == null && v.Status == VisitStatus.Open)
            .OrderBy(v => v.VisitDate)
            .ToListAsync();
        return View(pending);
    }

    public async Task<IActionResult> Record(int visitId)
    {
        var visit = await _db.Visits.Include(v => v.Patient).FirstOrDefaultAsync(v => v.Id == visitId);
        if (visit is null) return NotFound();
        ViewBag.Visit = visit;
        ViewBag.OpdRoles = StaffRoles.OpdRoles;
        return View(new Triage { VisitId = visitId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(Triage triage, string assignedOpdRole)
    {
        if (!StaffRoles.OpdRoles.Contains(assignedOpdRole))
        {
            ModelState.AddModelError("", "Please select which OPD department to send the patient to.");
            var visit = await _db.Visits.Include(v => v.Patient).FirstOrDefaultAsync(v => v.Id == triage.VisitId);
            ViewBag.Visit = visit;
            ViewBag.OpdRoles = StaffRoles.OpdRoles;
            return View(triage);
        }

        triage.RecordedAt = DateTime.UtcNow;
        _db.Triages.Add(triage);

        var targetVisit = await _db.Visits.FindAsync(triage.VisitId);
        if (targetVisit is not null)
        {
            targetVisit.Status = VisitStatus.InProgress;
            targetVisit.AssignedOpdRole = assignedOpdRole;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Vitals recorded. Patient sent to the selected OPD.";
        return RedirectToAction(nameof(Index));
    }
}