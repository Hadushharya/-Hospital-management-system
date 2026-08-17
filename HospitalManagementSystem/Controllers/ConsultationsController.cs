using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

// OPD Doctor module: consultation, plus branching into LabRequest and/or Prescription.
[Authorize(Roles = StaffRoles.OpdRolesCsv + "," + StaffRoles.Admin)]
public class ConsultationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ConsultationsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    //public async Task<IActionResult> Index()
    //{
    //    var queue = await _db.Visits
    //        .Include(v => v.Patient)
    //        .Include(v => v.Triage)
    //        .Where(v => v.Triage != null
    //                 && v.Status == VisitStatus.InProgress
    //                 && !v.Consultations.Any())
    //        .OrderBy(v => v.VisitDate)
    //        .ToListAsync();
    //    return View(queue);
    //}
    public async Task<IActionResult> Index()
    {
        var isAdmin = User.IsInRole(StaffRoles.Admin);
        var myOpdRole = StaffRoles.OpdRoles.FirstOrDefault(r => User.IsInRole(r));

        var query = _db.Visits
            .Include(v => v.Patient)
            .Include(v => v.Triage)
            .Where(v => v.Triage != null
                     && v.Status == VisitStatus.InProgress
                     && !v.Consultations.Any());

        // Each OPD role only sees patients Triage routed specifically to them.
        // Admin sees everyone, regardless of department.
        if (!isAdmin)
            query = query.Where(v => v.AssignedOpdRole == myOpdRole);

        var queue = await query.OrderBy(v => v.VisitDate).ToListAsync();
        return View(queue);
    }
    // Doctor's queue of consultations that have a lab result waiting to be reviewed.
    // Filters by the logged-in doctor's own Id (via Consultation.DoctorId), unless Admin.
    public async Task<IActionResult> ResultsQueue()
    {
        var doctorId = _userManager.GetUserId(User)!;
        var isAdmin = User.IsInRole(StaffRoles.Admin);

        var query = _db.Consultations
            .Include(c => c.Visit).ThenInclude(v => v.Patient)
            .Include(c => c.LabRequests).ThenInclude(lr => lr.LabResult)
            .Where(c => c.HasNewLabResult);

        if (!isAdmin)
            query = query.Where(c => c.DoctorId == doctorId);

        var queue = await query.OrderByDescending(c => c.LastLabResultAt).ToListAsync();
        return View(queue);
    }

    public async Task<IActionResult> Create(int visitId)
    {
        var visit = await _db.Visits.Include(v => v.Patient).Include(v => v.Triage)
            .FirstOrDefaultAsync(v => v.Id == visitId);
        if (visit is null) return NotFound();
        ViewBag.Visit = visit;
        return View(new Consultation { VisitId = visitId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Consultation consultation)
    {
        consultation.DoctorId = _userManager.GetUserId(User)!;
        consultation.ConsultationDate = DateTime.UtcNow;
        _db.Consultations.Add(consultation);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Consultation saved. You can now add a lab request and/or prescription.";
        return RedirectToAction(nameof(Manage), new { id = consultation.Id });
    }

    public async Task<IActionResult> Manage(int id)
    {
        var consultation = await _db.Consultations
            .Include(c => c.Visit).ThenInclude(v => v.Patient)
            .Include(c => c.LabRequests).ThenInclude(lr => lr.LabResult)
            .Include(c => c.Prescriptions).ThenInclude(p => p.PrescriptionItems).ThenInclude(pi => pi.Medicine)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (consultation is null) return NotFound();

        // Mark any newly-sent results as seen now that the doctor has opened the chart,
        // and clear the notification flag so it drops off ResultsQueue / any badge.
        var newlySent = consultation.LabRequests
            .Where(lr => lr.Status == LabRequestStatus.Completed && lr.DoctorReviewedAt == null)
            .ToList();
        if (newlySent.Count > 0 || consultation.HasNewLabResult)
        {
            foreach (var lr in newlySent) lr.DoctorReviewedAt = DateTime.UtcNow;
            consultation.HasNewLabResult = false;
            await _db.SaveChangesAsync();
        }

        ViewBag.Medicines = await _db.Medicines.Where(m => m.QuantityInStock > 0).OrderBy(m => m.Name).ToListAsync();
        ViewBag.LabFees = await _db.FeeSettings.Where(f => f.FeeType == FeeType.Lab && f.IsActive).OrderBy(f => f.PaymentName).ToListAsync();
        return View(consultation);
    }

    // Doctor orders one or more lab tests in a single submit — each is dispatched in
    // parallel to Reception (billing) and Lab. Fee amounts come from server-side
    // FeeSetting records, same as registration.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestLab(int consultationId, string[] testNames, int[] feeSettingIds)
    {
        var consultation = await _db.Consultations.FindAsync(consultationId);
        if (consultation is null) return NotFound();

        int created = 0;
        for (int i = 0; i < testNames.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(testNames[i])) continue;

            var fee = await _db.FeeSettings.FirstOrDefaultAsync(f => f.Id == feeSettingIds[i] && f.IsActive);
            if (fee is null) continue;

            var invoice = new Invoice
            {
                VisitId = consultation.VisitId,
                FeeSettingId = fee.Id,
                FeeType = FeeType.Lab,
                Amount = fee.Amount,
                Status = InvoiceStatus.Pending // Reception must collect this before Lab proceeds
            };
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync(); // generates invoice.Id

            _db.LabRequests.Add(new LabRequest
            {
                ConsultationId = consultationId,
                TestName = testNames[i],
                Status = LabRequestStatus.PaymentPending,
                InvoiceId = invoice.Id,
                RequestedAt = DateTime.UtcNow
            });
            created++;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = created switch
        {
            0 => "No valid tests were submitted.",
            1 => "Lab request sent to Reception (billing) and Lab simultaneously.",
            _ => $"{created} lab requests sent to Reception (billing) and Lab simultaneously."
        };
        return RedirectToAction(nameof(Manage), new { id = consultationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Prescribe(int consultationId, int[] medicineIds, int[] quantities)
    {
        var prescription = new Prescription
        {
            ConsultationId = consultationId,
            Status = PrescriptionStatus.Pending
        };
        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync(); // generates prescription.Id

        for (int i = 0; i < medicineIds.Length; i++)
        {
            _db.PrescriptionItems.Add(new PrescriptionItem
            {
                PrescriptionId = prescription.Id,
                MedicineId = medicineIds[i],
                Quantity = quantities[i]
            });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Prescription created. Sent to Pharmacy — patient must pay Cashier first.";
        return RedirectToAction(nameof(Manage), new { id = consultationId });
    }
    [Authorize(Roles = StaffRoles.OpdRolesCsv + "," + StaffRoles.Admin)]
    public async Task<IActionResult> History(string? q)
    {
        var patients = new List<Patient>();
        if (!string.IsNullOrWhiteSpace(q))
        {
            patients = await _db.Patients
                .Where(p => p.FullName.Contains(q) || p.Phone.Contains(q))
                .OrderBy(p => p.FullName)
                .ToListAsync();
        }
        ViewBag.Query = q;
        return View(patients);
    }

    [Authorize(Roles = StaffRoles.OpdRolesCsv + "," + StaffRoles.Admin)]
    public async Task<IActionResult> PatientHistory(int patientId)
    {
        var patient = await _db.Patients
            .Include(p => p.Visits).ThenInclude(v => v.Consultations).ThenInclude(c => c.LabRequests).ThenInclude(lr => lr.LabResult)
            .Include(p => p.Visits).ThenInclude(v => v.Consultations).ThenInclude(c => c.Prescriptions).ThenInclude(pr => pr.PrescriptionItems).ThenInclude(pi => pi.Medicine)
            .FirstOrDefaultAsync(p => p.Id == patientId);
        if (patient is null) return NotFound();
        return View(patient);
    }
}