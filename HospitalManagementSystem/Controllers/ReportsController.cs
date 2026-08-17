//using HospitalManagementSystem.Data;
//using HospitalManagementSystem.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace HospitalManagementSystem.Controllers;

//[Authorize(Roles = StaffRoles.Admin)]
//public class ReportsController : Controller
//{
//    private readonly ApplicationDbContext _db;
//    public ReportsController(ApplicationDbContext db) => _db = db;

//    // General Reports — flexible date range (pick one day for a daily report,
//    // or a full month/year range for a periodic report). Defaults to today.
//    public async Task<IActionResult> General(DateTime? start, DateTime? end)
//    {
//        var startDate = (start ?? DateTime.UtcNow).Date;
//        var endDateExclusive = (end ?? startDate).Date.AddDays(1); // inclusive end date, so +1 day for the < comparison

//        ViewBag.Start = startDate;
//        ViewBag.End = endDateExclusive.AddDays(-1);

//        ViewBag.PatientsRegistered = await _db.Patients
//            .CountAsync(p => p.CreatedAt >= startDate && p.CreatedAt < endDateExclusive);

//        ViewBag.VisitsCount = await _db.Visits
//            .CountAsync(v => v.VisitDate >= startDate && v.VisitDate < endDateExclusive);

//        var invoices = await _db.Invoices
//            .Where(i => i.Status == InvoiceStatus.Paid && i.CreatedAt >= startDate && i.CreatedAt < endDateExclusive)
//            .ToListAsync();
//        ViewBag.InvoicesByType = invoices
//            .GroupBy(i => i.FeeType)
//            .Select(g => new { FeeType = g.Key.ToString(), Count = g.Count(), Total = g.Sum(i => i.Amount) })
//            .ToList();
//        ViewBag.InvoiceTotal = invoices.Sum(i => i.Amount);

//        var payments = await _db.Payments
//            .Where(p => p.Status == PaymentStatus.Paid && p.PaidAt >= startDate && p.PaidAt < endDateExclusive)
//            .ToListAsync();
//        ViewBag.PaymentCount = payments.Count;
//        ViewBag.PaymentTotal = payments.Sum(p => p.Amount);

//        ViewBag.GrandTotal = (decimal)ViewBag.InvoiceTotal + (decimal)ViewBag.PaymentTotal;

//        return View();
//    }

//    // Profit report — Store Man's registered cost price vs. what Pharmacy actually
//    // dispensed in the range (StockTransactions of type StockOut), valued at the
//    // Medicine's current selling price. Profit = revenue - cost, per item and total.
//    public async Task<IActionResult> Profit(DateTime? start, DateTime? end)
//    {
//        var startDate = (start ?? DateTime.UtcNow.AddDays(-30)).Date;
//        var endDateExclusive = (end ?? DateTime.UtcNow).Date.AddDays(1);

//        ViewBag.Start = startDate;
//        ViewBag.End = endDateExclusive.AddDays(-1);

//        var dispensedTransactions = await _db.StockTransactions
//            .Where(st => st.ItemType == StockItemType.Medicine
//                      && st.TransactionType == StockTransactionType.StockOut
//                      && st.Timestamp >= startDate && st.Timestamp < endDateExclusive)
//            .ToListAsync();

//        var medicineIds = dispensedTransactions.Select(t => t.ItemId).Distinct().ToList();
//        var medicines = await _db.Medicines
//            .Where(m => medicineIds.Contains(m.Id))
//            .ToDictionaryAsync(m => m.Id);

//        var rows = dispensedTransactions
//            .GroupBy(t => t.ItemId)
//            .Where(g => medicines.ContainsKey(g.Key))
//            .Select(g =>
//            {
//                var medicine = medicines[g.Key];
//                var qty = g.Sum(t => t.Quantity);
//                var cost = qty * medicine.CostPrice;
//                var revenue = qty * medicine.SellingPrice;
//                return new
//                {
//                    MedicineName = medicine.Name,
//                    QuantityDispensed = qty,
//                    CostPrice = medicine.CostPrice,
//                    SellingPrice = medicine.SellingPrice,
//                    TotalCost = cost,
//                    TotalRevenue = revenue,
//                    Profit = revenue - cost
//                };
//            })
//            .OrderByDescending(r => r.Profit)
//            .ToList();

//        ViewBag.Rows = rows;
//        ViewBag.TotalCost = rows.Sum(r => r.TotalCost);
//        ViewBag.TotalRevenue = rows.Sum(r => r.TotalRevenue);
//        ViewBag.TotalProfit = rows.Sum(r => r.Profit);

//        // Actual cash collected by Cashier in the same range, for comparison —
//        // may differ from catalog revenue above if a cashier entered a custom amount.
//        ViewBag.ActualCollected = await _db.Payments
//            .Where(p => p.Status == PaymentStatus.Paid && p.PaidAt >= startDate && p.PaidAt < endDateExclusive)
//            .SumAsync(p => p.Amount);

//        return View();
//    }
//    // Reception's own daily report — registrations + fees they collected today.
//    // Separate from the Admin General report so Reception doesn't see
//    // cross-department (Pharmacy/Cashier) financials.
//    public async Task<IActionResult> ReceptionDaily(DateTime? date)
//    {
//        var day = (date ?? DateTime.UtcNow).Date;
//        var nextDay = day.AddDays(1);

//        ViewBag.ReportDate = day;

//        ViewBag.PatientsRegistered = await _db.Patients
//            .CountAsync(p => p.CreatedAt >= day && p.CreatedAt < nextDay);

//        var invoices = await _db.Invoices
//            .Where(i => i.Status == InvoiceStatus.Paid && i.CreatedAt >= day && i.CreatedAt < nextDay)
//            .Include(i => i.FeeSetting)
//            .ToListAsync();

//        ViewBag.Invoices = invoices;
//        ViewBag.InvoiceTotal = invoices.Sum(i => i.Amount);

//        ViewBag.Hospital = await _db.HospitalSettings.FirstOrDefaultAsync() ?? new HospitalSettings();

//        return View();
//    }
//}
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

// No class-level [Authorize] here on purpose — General/Profit are Admin-only,
// ReceptionDaily is Receptionist+Admin. Each action sets its own roles below.
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ReportsController(ApplicationDbContext db) => _db = db;

    // General Reports — flexible date range (pick one day for a daily report,
    // or a full month/year range for a periodic report). Defaults to today.
    [Authorize(Roles = StaffRoles.Admin)]
    public async Task<IActionResult> General(DateTime? start, DateTime? end)
    {
        var startDate = (start ?? DateTime.UtcNow).Date;
        var endDateExclusive = (end ?? startDate).Date.AddDays(1); // inclusive end date, so +1 day for the < comparison

        ViewBag.Start = startDate;
        ViewBag.End = endDateExclusive.AddDays(-1);

        ViewBag.PatientsRegistered = await _db.Patients
            .CountAsync(p => p.CreatedAt >= startDate && p.CreatedAt < endDateExclusive);

        ViewBag.VisitsCount = await _db.Visits
            .CountAsync(v => v.VisitDate >= startDate && v.VisitDate < endDateExclusive);

        var invoices = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid && i.CreatedAt >= startDate && i.CreatedAt < endDateExclusive)
            .ToListAsync();
        ViewBag.InvoicesByType = invoices
            .GroupBy(i => i.FeeType)
            .Select(g => new { FeeType = g.Key.ToString(), Count = g.Count(), Total = g.Sum(i => i.Amount) })
            .ToList();
        ViewBag.InvoiceTotal = invoices.Sum(i => i.Amount);

        var payments = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Paid && p.PaidAt >= startDate && p.PaidAt < endDateExclusive)
            .ToListAsync();
        ViewBag.PaymentCount = payments.Count;
        ViewBag.PaymentTotal = payments.Sum(p => p.Amount);

        ViewBag.GrandTotal = (decimal)ViewBag.InvoiceTotal + (decimal)ViewBag.PaymentTotal;

        return View();
    }

    // Profit report — Store Man's registered cost price vs. what Pharmacy actually
    // dispensed in the range (StockTransactions of type StockOut), valued at the
    // Medicine's current selling price. Profit = revenue - cost, per item and total.
    [Authorize(Roles = StaffRoles.Admin)]
    public async Task<IActionResult> Profit(DateTime? start, DateTime? end)
    {
        var startDate = (start ?? DateTime.UtcNow.AddDays(-30)).Date;
        var endDateExclusive = (end ?? DateTime.UtcNow).Date.AddDays(1);

        ViewBag.Start = startDate;
        ViewBag.End = endDateExclusive.AddDays(-1);

        var dispensedTransactions = await _db.StockTransactions
            .Where(st => st.ItemType == StockItemType.Medicine
                      && st.TransactionType == StockTransactionType.StockOut
                      && st.Timestamp >= startDate && st.Timestamp < endDateExclusive)
            .ToListAsync();

        var medicineIds = dispensedTransactions.Select(t => t.ItemId).Distinct().ToList();
        var medicines = await _db.Medicines
            .Where(m => medicineIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);

        var rows = dispensedTransactions
            .GroupBy(t => t.ItemId)
            .Where(g => medicines.ContainsKey(g.Key))
            .Select(g =>
            {
                var medicine = medicines[g.Key];
                var qty = g.Sum(t => t.Quantity);
                var cost = qty * medicine.CostPrice;
                var revenue = qty * medicine.SellingPrice;
                return new
                {
                    MedicineName = medicine.Name,
                    QuantityDispensed = qty,
                    CostPrice = medicine.CostPrice,
                    SellingPrice = medicine.SellingPrice,
                    TotalCost = cost,
                    TotalRevenue = revenue,
                    Profit = revenue - cost
                };
            })
            .OrderByDescending(r => r.Profit)
            .ToList();

        ViewBag.Rows = rows;
        ViewBag.TotalCost = rows.Sum(r => r.TotalCost);
        ViewBag.TotalRevenue = rows.Sum(r => r.TotalRevenue);
        ViewBag.TotalProfit = rows.Sum(r => r.Profit);

        // Actual cash collected by Cashier in the same range, for comparison —
        // may differ from catalog revenue above if a cashier entered a custom amount.
        ViewBag.ActualCollected = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Paid && p.PaidAt >= startDate && p.PaidAt < endDateExclusive)
            .SumAsync(p => p.Amount);

        return View();
    }

    // Reception's own daily report — registrations + fees they collected today.
    // Separate from the Admin General report so Reception doesn't see
    // cross-department (Pharmacy/Cashier) financials.
    [Authorize(Roles = StaffRoles.Receptionist + "," + StaffRoles.Admin)]
    public async Task<IActionResult> ReceptionDaily(DateTime? date)
    {
        var day = (date ?? DateTime.UtcNow).Date;
        var nextDay = day.AddDays(1);

        ViewBag.ReportDate = day;

        ViewBag.PatientsRegistered = await _db.Patients
            .CountAsync(p => p.CreatedAt >= day && p.CreatedAt < nextDay);

        var invoices = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid && i.CreatedAt >= day && i.CreatedAt < nextDay)
            .Include(i => i.FeeSetting)
            .ToListAsync();

        ViewBag.Invoices = invoices;
        ViewBag.InvoiceTotal = invoices.Sum(i => i.Amount);

        // Breakdown by payment method — helps Reception reconcile the cash
        // drawer against what was actually collected in cash vs. bank transfer.
        ViewBag.CashTotal = invoices.Where(i => i.PaymentMethod == PaymentMethod.Cash).Sum(i => i.Amount);
        ViewBag.BankTransferTotal = invoices.Where(i => i.PaymentMethod == PaymentMethod.BankTransfer).Sum(i => i.Amount);

        ViewBag.Hospital = await _db.HospitalSettings.FirstOrDefaultAsync() ?? new HospitalSettings();

        return View();
    }
}