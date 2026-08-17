//using HospitalManagementSystem.Data;
//using HospitalManagementSystem.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace HospitalManagementSystem.Controllers;

//public class PharmacyController : Controller
//{
//    private readonly ApplicationDbContext _db;
//    private readonly UserManager<ApplicationUser> _userManager;

//    public PharmacyController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
//    {
//        _db = db;
//        _userManager = userManager;
//    }

//    [Authorize(Roles = StaffRoles.Cashier + "," + StaffRoles.Admin)]
//    public async Task<IActionResult> PendingPayments()
//    {
//        var pending = await _db.Prescriptions
//            .Include(p => p.PrescriptionItems).ThenInclude(pi => pi.Medicine)
//            .Include(p => p.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
//            .Where(p => p.Status == PrescriptionStatus.Pending)
//            .ToListAsync();
//        return View(pending);
//    }

//    [Authorize(Roles = StaffRoles.Cashier + "," + StaffRoles.Admin)]
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> CollectPayment(int prescriptionId, decimal amount, PaymentMethod paymentMethod)
//    {
//        var prescription = await _db.Prescriptions.FindAsync(prescriptionId);
//        if (prescription is null) return NotFound();

//        var payment = new Payment
//        {
//            PrescriptionId = prescriptionId,
//            Amount = amount,
//            Status = PaymentStatus.Paid,
//            PaymentMethod = paymentMethod,
//            CashierId = _userManager.GetUserId(User)!,
//            PaidAt = DateTime.UtcNow
//        };
//        _db.Payments.Add(payment);
//        prescription.Status = PrescriptionStatus.Paid;
//        await _db.SaveChangesAsync(); // generates payment.Id

//        // Auto receipt/ref number derived from the generated Id — sequential, unique,
//        // no separate counter table needed and no race condition.
//        payment.ReceiptNo = $"RCPT-{payment.Id:D6}";
//        await _db.SaveChangesAsync();

//        TempData["Success"] = "Payment collected. Receipt ready to print — send patient to Pharmacy for dispensing.";
//        return RedirectToAction(nameof(Receipt), new { paymentId = payment.Id });
//    }

//    [Authorize(Roles = StaffRoles.Cashier + "," + StaffRoles.Admin)]
//    public async Task<IActionResult> Receipt(int paymentId)
//    {
//        var payment = await _db.Payments
//            .Include(p => p.Prescription).ThenInclude(pr => pr.PrescriptionItems).ThenInclude(pi => pi.Medicine)
//            .Include(p => p.Prescription).ThenInclude(pr => pr.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
//            .FirstOrDefaultAsync(p => p.Id == paymentId);
//        if (payment is null) return NotFound();
//        return View(payment);
//    }

//    [Authorize(Roles = StaffRoles.Pharmacist + "," + StaffRoles.Admin)]
//    public async Task<IActionResult> ReadyToDispense()
//    {
//        var ready = await _db.Prescriptions
//            .Include(p => p.PrescriptionItems).ThenInclude(pi => pi.Medicine)
//            .Include(p => p.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
//            .Where(p => p.Status == PrescriptionStatus.Paid)
//            .ToListAsync();

//        // Visible so the pharmacist knows what's coming and can tell the patient
//        // to go pay at Cashier first — not dispensable yet.
//        ViewBag.AwaitingPayment = await _db.Prescriptions
//            .Include(p => p.PrescriptionItems).ThenInclude(pi => pi.Medicine)
//            .Include(p => p.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
//            .Where(p => p.Status == PrescriptionStatus.Pending)
//            .ToListAsync();

//        return View(ready);
//    }

//    [Authorize(Roles = StaffRoles.Pharmacist + "," + StaffRoles.Admin)]
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> Dispense(int prescriptionId)
//    {
//        var prescription = await _db.Prescriptions
//            .Include(p => p.PrescriptionItems).ThenInclude(pi => pi.Medicine)
//            .FirstOrDefaultAsync(p => p.Id == prescriptionId);

//        if (prescription is null || prescription.Status != PrescriptionStatus.Paid) return NotFound();

//        var pharmacistId = _userManager.GetUserId(User)!;

//        foreach (var item in prescription.PrescriptionItems)
//        {
//            if (item.Medicine.QuantityInStock < item.Quantity)
//            {
//                TempData["Error"] = $"Insufficient stock for {item.Medicine.Name}. Dispensing cancelled.";
//                return RedirectToAction(nameof(ReadyToDispense));
//            }
//        }

//        foreach (var item in prescription.PrescriptionItems)
//        {
//            item.Medicine.QuantityInStock -= item.Quantity;

//            _db.StockTransactions.Add(new StockTransaction
//            {
//                ItemType = StockItemType.Medicine,
//                ItemId = item.MedicineId,
//                TransactionType = StockTransactionType.StockOut,
//                Quantity = item.Quantity,
//                ReferenceNote = $"Dispensed for Prescription {prescription.Id}",
//                StaffId = pharmacistId,
//                Timestamp = DateTime.UtcNow
//            });
//        }

//        prescription.Status = PrescriptionStatus.Dispensed;
//        await _db.SaveChangesAsync();

//        var lowStockItems = prescription.PrescriptionItems
//            .Where(i => i.Medicine.QuantityInStock <= i.Medicine.ReorderLevel)
//            .Select(i => i.Medicine.Name)
//            .ToList();
//        if (lowStockItems.Count > 0)
//        {
//            TempData["Warning"] = $"Low stock alert (visible to Store Man): {string.Join(", ", lowStockItems)}";
//        }

//        TempData["Success"] = "Medicines dispensed. Stock updated automatically.";
//        return RedirectToAction(nameof(ReadyToDispense));
//    }
//}
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

public class PharmacyController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PharmacyController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize(Roles = StaffRoles.Cashier + "," + StaffRoles.Admin)]
    public async Task<IActionResult> PendingPayments()
    {
        var pending = await _db.Prescriptions
            .Include(p => p.PrescriptionItems).ThenInclude(pi => pi.Medicine)
            .Include(p => p.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
            .Where(p => p.Status == PrescriptionStatus.Pending)
            .ToListAsync();
        return View(pending);
    }

    [Authorize(Roles = StaffRoles.Cashier + "," + StaffRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CollectPayment(int prescriptionId, decimal amount, PaymentMethod paymentMethod)
    {
        var prescription = await _db.Prescriptions.FindAsync(prescriptionId);
        if (prescription is null) return NotFound();

        var payment = new Payment
        {
            PrescriptionId = prescriptionId,
            Amount = amount,
            Status = PaymentStatus.Paid,
            PaymentMethod = paymentMethod,
            CashierId = _userManager.GetUserId(User)!,
            PaidAt = DateTime.UtcNow
        };

        // Random, non-sequential receipt number — not derived from the database Id,
        // so it can't be used to infer transaction volume or guessed/incremented.
        string receiptNo;
        do
        {
            receiptNo = GenerateReceiptNo();
        } while (await _db.Payments.AnyAsync(p => p.ReceiptNo == receiptNo));
        payment.ReceiptNo = receiptNo;

        _db.Payments.Add(payment);
        prescription.Status = PrescriptionStatus.Paid;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Payment collected. Receipt ready to print — send patient to Pharmacy for dispensing.";
        return RedirectToAction(nameof(Receipt), new { paymentId = payment.Id });
    }

    private static string GenerateReceiptNo()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no O/0/I/1 to avoid confusion
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[8];
        rng.GetBytes(bytes);
        var code = new char[8];
        for (int i = 0; i < 8; i++)
            code[i] = chars[bytes[i] % chars.Length];
        return "RCPT-" + new string(code);
    }

    [Authorize(Roles = StaffRoles.Cashier + "," + StaffRoles.Admin)]
    public async Task<IActionResult> Receipt(int paymentId)
    {
        var payment = await _db.Payments
            .Include(p => p.Prescription).ThenInclude(pr => pr.PrescriptionItems).ThenInclude(pi => pi.Medicine)
            .Include(p => p.Prescription).ThenInclude(pr => pr.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
            .FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment is null) return NotFound();

        ViewBag.Hospital = await _db.HospitalSettings.FirstOrDefaultAsync() ?? new HospitalSettings();
        return View(payment);
    }

    [Authorize(Roles = StaffRoles.Pharmacist + "," + StaffRoles.Admin)]
    public async Task<IActionResult> ReadyToDispense()
    {
        var ready = await _db.Prescriptions
            .Include(p => p.PrescriptionItems).ThenInclude(pi => pi.Medicine)
            .Include(p => p.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
            .Where(p => p.Status == PrescriptionStatus.Paid)
            .ToListAsync();

        // Visible so the pharmacist knows what's coming and can tell the patient
        // to go pay at Cashier first — not dispensable yet.
        ViewBag.AwaitingPayment = await _db.Prescriptions
            .Include(p => p.PrescriptionItems).ThenInclude(pi => pi.Medicine)
            .Include(p => p.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
            .Where(p => p.Status == PrescriptionStatus.Pending)
            .ToListAsync();

        return View(ready);
    }

    [Authorize(Roles = StaffRoles.Pharmacist + "," + StaffRoles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dispense(int prescriptionId)
    {
        var prescription = await _db.Prescriptions
            .Include(p => p.PrescriptionItems).ThenInclude(pi => pi.Medicine)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId);

        if (prescription is null || prescription.Status != PrescriptionStatus.Paid) return NotFound();

        var pharmacistId = _userManager.GetUserId(User)!;

        foreach (var item in prescription.PrescriptionItems)
        {
            if (item.Medicine.QuantityInStock < item.Quantity)
            {
                TempData["Error"] = $"Insufficient stock for {item.Medicine.Name}. Dispensing cancelled.";
                return RedirectToAction(nameof(ReadyToDispense));
            }
        }

        foreach (var item in prescription.PrescriptionItems)
        {
            item.Medicine.QuantityInStock -= item.Quantity;

            _db.StockTransactions.Add(new StockTransaction
            {
                ItemType = StockItemType.Medicine,
                ItemId = item.MedicineId,
                TransactionType = StockTransactionType.StockOut,
                Quantity = item.Quantity,
                ReferenceNote = $"Dispensed for Prescription {prescription.Id}",
                StaffId = pharmacistId,
                Timestamp = DateTime.UtcNow
            });
        }

        prescription.Status = PrescriptionStatus.Dispensed;
        await _db.SaveChangesAsync();

        var lowStockItems = prescription.PrescriptionItems
            .Where(i => i.Medicine.QuantityInStock <= i.Medicine.ReorderLevel)
            .Select(i => i.Medicine.Name)
            .ToList();
        if (lowStockItems.Count > 0)
        {
            TempData["Warning"] = $"Low stock alert (visible to Store Man): {string.Join(", ", lowStockItems)}";
        }

        TempData["Success"] = "Medicines dispensed. Stock updated automatically.";
        return RedirectToAction(nameof(ReadyToDispense));
    }
}