using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize(Roles = StaffRoles.Receptionist + "," + StaffRoles.Admin)]
    public async Task<IActionResult> Reception()
    {
        var today = DateTime.UtcNow.Date;

        ViewBag.RegisteredToday = await _db.Patients.CountAsync(p => p.CreatedAt.Date == today);

        var pending = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Pending)
            .ToListAsync();
        ViewBag.PendingInvoiceCount = pending.Count;
        ViewBag.PendingInvoiceTotal = pending.Sum(i => i.Amount);

        var collectedToday = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid && i.CreatedAt.Date == today)
            .ToListAsync();
        ViewBag.CollectedTodayTotal = collectedToday.Sum(i => i.Amount);

        ViewBag.RecentPatients = await _db.Patients
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .ToListAsync();

        return View();
    }

    [Authorize(Roles = StaffRoles.OpdRolesCsv + "," + StaffRoles.Admin)]
    public async Task<IActionResult> OPD()
    {
        var today = DateTime.UtcNow.Date;
        var doctorId = User.IsInRole(StaffRoles.Admin) ? null : _userManager.GetUserId(User);
        var isAdmin = User.IsInRole(StaffRoles.Admin);
        var myOpdRole = StaffRoles.OpdRoles.FirstOrDefault(r => User.IsInRole(r));
        //ViewBag.WaitingInQueue = await _db.Visits
        //    .CountAsync(v => v.Triage != null
        //                   && v.Status == VisitStatus.InProgress
        //                   && !v.Consultations.Any());
        ViewBag.WaitingInQueue = await _db.Visits
            .CountAsync(v => v.Triage != null
                           && v.Status == VisitStatus.InProgress
                           && !v.Consultations.Any()
                           && (isAdmin || v.AssignedOpdRole == myOpdRole));

        var consultationsQuery = _db.Consultations.Where(c => c.ConsultationDate.Date == today);
        if (doctorId != null)
            consultationsQuery = consultationsQuery.Where(c => c.DoctorId == doctorId);
        ViewBag.ConsultationsToday = await consultationsQuery.CountAsync();

        var labFeesActive = await _db.FeeSettings.CountAsync(f => f.FeeType == FeeType.Lab && f.IsActive);
        ViewBag.LabFeesConfigured = labFeesActive;

        //ViewBag.RecentQueue = await _db.Visits
        //    .Include(v => v.Patient)
        //    .Include(v => v.Triage)
        //    .Where(v => v.Triage != null && v.Status == VisitStatus.InProgress && !v.Consultations.Any())
        //    .OrderBy(v => v.VisitDate)
        //    .Take(5)
        //    .ToListAsync

       

        var recentQueueQuery = _db.Visits
            .Include(v => v.Patient)
            .Include(v => v.Triage)
            .Where(v => v.Triage != null && v.Status == VisitStatus.InProgress && !v.Consultations.Any());

        if (!isAdmin)
            recentQueueQuery = recentQueueQuery.Where(v => v.AssignedOpdRole == myOpdRole);

        ViewBag.RecentQueue = await recentQueueQuery
            .OrderBy(v => v.VisitDate)
            .Take(5)
            .ToListAsync();

        var recentQuery = _db.Consultations
            .Include(c => c.Visit).ThenInclude(v => v.Patient)
            .Where(c => true);
        if (doctorId != null)
            recentQuery = recentQuery.Where(c => c.DoctorId == doctorId);
        ViewBag.RecentConsultations = await recentQuery
            .OrderByDescending(c => c.ConsultationDate)
            .Take(5)
            .ToListAsync();

        return View();
    }
    [Authorize(Roles = StaffRoles.TriageNurse + "," + StaffRoles.Admin)]
    public async Task<IActionResult> Triage()
    {
        var today = DateTime.UtcNow.Date;

        ViewBag.WaitingForVitals = await _db.Visits
            .CountAsync(v => v.Triage == null && v.Status == VisitStatus.Open);

        ViewBag.RecordedToday = await _db.Triages
            .CountAsync(t => t.RecordedAt.Date == today);

        ViewBag.RecentQueue = await _db.Visits
            .Include(v => v.Patient)
            .Where(v => v.Triage == null && v.Status == VisitStatus.Open)
            .OrderBy(v => v.VisitDate)
            .Take(5)
            .ToListAsync();

        return View();
    }
    [Authorize(Roles = StaffRoles.LabTechnician + "," + StaffRoles.Admin)]
    public async Task<IActionResult> Lab()
    {
        var today = DateTime.UtcNow.Date;

        ViewBag.PaidQueueCount = await _db.LabRequests
            .CountAsync(lr => lr.Status == LabRequestStatus.PaymentConfirmed);

        ViewBag.ReadyToSendCount = await _db.LabRequests
            .CountAsync(lr => lr.Status == LabRequestStatus.ResultEntered);

        ViewBag.SentToday = await _db.LabRequests
            .CountAsync(lr => lr.Status == LabRequestStatus.Completed && lr.SentAt != null && lr.SentAt.Value.Date == today);

        ViewBag.RecentQueue = await _db.LabRequests
            .Include(lr => lr.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
            .Where(lr => lr.Status == LabRequestStatus.PaymentConfirmed)
            .OrderBy(lr => lr.RequestedAt)
            .Take(5)
            .ToListAsync();

        return View();
    }
    [Authorize(Roles = StaffRoles.Pharmacist + "," + StaffRoles.Admin)]
    public async Task<IActionResult> Pharmacy()
    {
        var today = DateTime.UtcNow.Date;

        ViewBag.ReadyToDispenseCount = await _db.Prescriptions
            .CountAsync(p => p.Status == PrescriptionStatus.Paid);

        ViewBag.DispensedToday = await _db.Prescriptions
            .Include(p => p.PrescriptionItems)
            .Where(p => p.Status == PrescriptionStatus.Dispensed)
            .CountAsync();

        ViewBag.LowStockCount = await _db.Medicines
            .CountAsync(m => m.QuantityInStock <= m.ReorderLevel);

        ViewBag.RecentQueue = await _db.Prescriptions
            .Include(p => p.PrescriptionItems).ThenInclude(pi => pi.Medicine)
            .Include(p => p.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
            .Where(p => p.Status == PrescriptionStatus.Paid)
            .Take(5)
            .ToListAsync();

        return View();
    }
    [Authorize(Roles = StaffRoles.Cashier + "," + StaffRoles.Admin)]
    public async Task<IActionResult> Cashier()
    {
        var today = DateTime.UtcNow.Date;

        ViewBag.PendingPaymentsCount = await _db.Prescriptions
            .CountAsync(p => p.Status == PrescriptionStatus.Pending);

        var collectedToday = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Paid && p.PaidAt.Date == today)
            .ToListAsync();
        ViewBag.CollectedTodayCount = collectedToday.Count;
        ViewBag.CollectedTodayTotal = collectedToday.Sum(p => p.Amount);

        ViewBag.RecentQueue = await _db.Prescriptions
            .Include(p => p.PrescriptionItems).ThenInclude(pi => pi.Medicine)
            .Include(p => p.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
            .Where(p => p.Status == PrescriptionStatus.Pending)
            .Take(5)
            .ToListAsync();

        return View();
    }


    [Authorize(Roles = StaffRoles.Admin)]
    public async Task<IActionResult> Admin()
    {
        ViewBag.TotalUsers = _userManager.Users.Count();
        ViewBag.ActiveUsers = _userManager.Users.Count(u => u.IsActive);

        ViewBag.TotalPatients = await _db.Patients.CountAsync();

        var today = DateTime.UtcNow.Date;
        ViewBag.VisitsToday = await _db.Visits.CountAsync(v => v.VisitDate.Date == today);

        ViewBag.PendingInvoiceTotal = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Pending)
            .SumAsync(i => i.Amount);

        var paidToday = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid && i.CreatedAt.Date == today)
            .SumAsync(i => i.Amount);
        var collectedToday = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Paid && p.PaidAt.Date == today)
            .SumAsync(p => p.Amount);
        ViewBag.RevenueToday = paidToday + collectedToday;

        return View();
    }
}