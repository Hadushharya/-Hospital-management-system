using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

[Authorize(Roles = StaffRoles.Receptionist + "," + StaffRoles.Admin)]
public class InvoicesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public InvoicesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Grouped by visit so Reception sees one card per patient, not one row per test.
    public async Task<IActionResult> Pending()
    {
        var pending = await _db.Invoices
            .Include(i => i.Visit).ThenInclude(v => v.Patient)
            .Include(i => i.FeeSetting)
            .Where(i => i.Status == InvoiceStatus.Pending)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();

        var grouped = pending.GroupBy(i => i.VisitId).ToList();
        return View(grouped);
    }

    // Collects every pending fee for a visit in one click, with one shared payment method,
    // then sends Reception straight to the printable receipt.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CollectAll(int visitId, PaymentMethod paymentMethod)
    {
        var invoices = await _db.Invoices
            .Include(i => i.LabRequest)
            .Where(i => i.VisitId == visitId && i.Status == InvoiceStatus.Pending)
            .ToListAsync();

        if (invoices.Count == 0) return NotFound();

        var collectedBy = _userManager.GetUserId(User);
        foreach (var invoice in invoices)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaymentMethod = paymentMethod;
            invoice.CollectedByUserId = collectedBy;

            if (invoice.LabRequest is not null)
            {
                invoice.LabRequest.Status = LabRequestStatus.PaymentConfirmed;
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"{invoices.Count} fee(s) collected. Receipt ready to print — send patient to the Lab.";
        return RedirectToAction(nameof(Receipt), new { visitId });
    }

    // Printable thermal-receipt page shown right after CollectAll.
    public async Task<IActionResult> Receipt(int visitId)
    {
        var visit = await _db.Visits
            .Include(v => v.Patient)
            .Include(v => v.Invoices).ThenInclude(i => i.FeeSetting)
            .FirstOrDefaultAsync(v => v.Id == visitId);
        if (visit is null) return NotFound();

        ViewBag.PaidInvoices = visit.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid)
            .OrderBy(i => i.CreatedAt)
            .ToList();
        return View(visit);
    }
}