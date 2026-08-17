using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

// Reception module: register patient, open a visit, collect the registration fee.
[Authorize(Roles = StaffRoles.Receptionist + "," + StaffRoles.Admin)]
public class PatientsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PatientsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var patients = await _db.Patients
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(patients);
    }

    public async Task<IActionResult> Register()
    {
        // Only active Registration-type fees are offered — Reception cannot type a custom amount.
        ViewBag.RegistrationFees = await _db.FeeSettings
            .Where(f => f.FeeType == FeeType.Registration && f.IsActive)
            .OrderBy(f => f.PaymentName)
            .ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(Patient patient, int feeSettingId, PaymentMethod paymentMethod)
    {
        var fee = await _db.FeeSettings.FirstOrDefaultAsync(f => f.Id == feeSettingId && f.IsActive);
        if (fee is null)
        {
            ModelState.AddModelError("", "Selected fee is not valid. Please choose a fee from the list.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.RegistrationFees = await _db.FeeSettings
                .Where(f => f.FeeType == FeeType.Registration && f.IsActive)
                .OrderBy(f => f.PaymentName)
                .ToListAsync();
            return View(patient);
        }

        patient.CreatedAt = DateTime.UtcNow;
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(); // generates patient.Id

        var visit = new Visit
        {
            PatientId = patient.Id,
            VisitDate = DateTime.UtcNow,
            Status = VisitStatus.Open
        };
        _db.Visits.Add(visit);
        await _db.SaveChangesAsync(); // generates visit.Id

        // Amount always comes from the server-side FeeSetting record, never from client input.
        var invoice = new Invoice
        {
            VisitId = visit.Id,
            FeeSettingId = fee!.Id,
            FeeType = fee.FeeType,
            Amount = fee.Amount,
            Status = InvoiceStatus.Paid,
            PaymentMethod = paymentMethod,
            CollectedByUserId = _userManager.GetUserId(User),
            CreatedAt = DateTime.UtcNow
        };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        // Confirmation/payment-complete screen, not a silent redirect —
        // Reception explicitly confirms before sending the patient onward to Triage.
        return RedirectToAction(nameof(Confirmation), new { visitId = visit.Id });
    }

    public async Task<IActionResult> Confirmation(int visitId)
    {
        var visit = await _db.Visits
            .Include(v => v.Patient)
            .Include(v => v.Invoices).ThenInclude(i => i.FeeSetting)
            .FirstOrDefaultAsync(v => v.Id == visitId);
        if (visit is null) return NotFound();
        return View(visit);
    }

    // Explicit "send to Triage" action — visit is already Open/InProgress by default,
    // this is just the deliberate hand-off click from Reception's side.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendToTriage(int visitId)
    {
        var visit = await _db.Visits.FindAsync(visitId);
        if (visit is null) return NotFound();
        visit.Status = VisitStatus.Open; // Triage queue shows Status == Open with no Triage yet
        await _db.SaveChangesAsync();
        TempData["Success"] = "Patient sent to Triage.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var patient = await _db.Patients
            .Include(p => p.Visits)
            .ThenInclude(v => v.Invoices)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient is null) return NotFound();
        return View(patient);
    }
    // Search existing patients by name or phone — used from the Reception dashboard
    public async Task<IActionResult> Search(string? q)
    {
        var results = new List<Patient>();
        if (!string.IsNullOrWhiteSpace(q))
        {
            results = await _db.Patients
                .Where(p => p.FullName.Contains(q) || p.Phone.Contains(q))
                .OrderBy(p => p.FullName)
                .ToListAsync();
        }

        var patientIds = results.Select(p => p.Id).ToList();
        var lastVisits = await _db.Visits
            .Where(v => patientIds.Contains(v.PatientId))
            .GroupBy(v => v.PatientId)
            .Select(g => new { PatientId = g.Key, LastVisitDate = g.Max(v => v.VisitDate) })
            .ToDictionaryAsync(x => x.PatientId, x => x.LastVisitDate);

        ViewBag.LastVisits = lastVisits;
        ViewBag.Query = q;
        return View(results);
    }

    // Sends an existing patient straight to Triage, no new payment — only allowed
    // if their most recent visit was within the last 5 days.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendToTriageFree(int patientId)
    {
        var lastVisit = await _db.Visits
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.VisitDate)
            .FirstOrDefaultAsync();

        if (lastVisit is null || (DateTime.UtcNow - lastVisit.VisitDate).TotalDays > 5)
        {
            TempData["Error"] = "Last visit was more than 5 days ago — a new registration payment is required.";
            return RedirectToAction(nameof(Revisit), new { patientId });
        }

        var visit = new Visit
        {
            PatientId = patientId,
            VisitDate = DateTime.UtcNow,
            Status = VisitStatus.Open
        };
        _db.Visits.Add(visit);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Patient sent to Triage — no new payment required (within the 5-day window).";
        return RedirectToAction(nameof(Index));
    }

    // Existing patient, but last visit was too long ago (or first visit) —
    // requires a fresh registration fee, same as new registration but skips
    // re-creating the Patient record.
    public async Task<IActionResult> Revisit(int patientId)
    {
        var patient = await _db.Patients.FindAsync(patientId);
        if (patient is null) return NotFound();
        ViewBag.Patient = patient;
        ViewBag.RegistrationFees = await _db.FeeSettings
            .Where(f => f.FeeType == FeeType.Registration && f.IsActive)
            .OrderBy(f => f.PaymentName)
            .ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revisit(int patientId, int feeSettingId, PaymentMethod paymentMethod)
    {
        var fee = await _db.FeeSettings.FirstOrDefaultAsync(f => f.Id == feeSettingId && f.IsActive);
        if (fee is null)
        {
            TempData["Error"] = "Please select a valid fee.";
            return RedirectToAction(nameof(Revisit), new { patientId });
        }

        var visit = new Visit
        {
            PatientId = patientId,
            VisitDate = DateTime.UtcNow,
            Status = VisitStatus.Open
        };
        _db.Visits.Add(visit);
        await _db.SaveChangesAsync();

        var invoice = new Invoice
        {
            VisitId = visit.Id,
            FeeSettingId = fee.Id,
            FeeType = fee.FeeType,
            Amount = fee.Amount,
            Status = InvoiceStatus.Paid,
            PaymentMethod = paymentMethod,
            CollectedByUserId = _userManager.GetUserId(User),
            CreatedAt = DateTime.UtcNow
        };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Confirmation), new { visitId = visit.Id });
    }
}
