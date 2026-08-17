using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

[Authorize(Roles = StaffRoles.StoreMan + "," + StaffRoles.Admin)]
public class MedicinesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public MedicinesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var medicines = await _db.Medicines.Include(m => m.Supplier).OrderBy(m => m.Name).ToListAsync();
        return View(medicines);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Suppliers = await _db.Suppliers.OrderBy(s => s.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Medicine medicine)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Suppliers = await _db.Suppliers.OrderBy(s => s.Name).ToListAsync();
            return View(medicine);
        }

        medicine.RegisteredByUserId = _userManager.GetUserId(User)!;
        medicine.DateReceived = DateTime.UtcNow;
        _db.Medicines.Add(medicine);
        await _db.SaveChangesAsync(); // generates medicine.Id

        _db.StockTransactions.Add(new StockTransaction
        {
            ItemType = StockItemType.Medicine,
            ItemId = medicine.Id,
            TransactionType = StockTransactionType.StockIn,
            Quantity = medicine.QuantityInStock,
            ReferenceNote = "Initial registration",
            StaffId = medicine.RegisteredByUserId,
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = $"{medicine.Name} registered and available to Pharmacy.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> LowStock()
    {
        var lowStock = await _db.Medicines
            .Where(m => m.QuantityInStock <= m.ReorderLevel)
            .OrderBy(m => m.QuantityInStock)
            .ToListAsync();
        return View(lowStock);
    }
}
