using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

[Authorize(Roles = StaffRoles.StoreMan + "," + StaffRoles.Admin)]
public class MedicalGoodsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public MedicalGoodsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var goods = await _db.MedicalGoods.Include(g => g.Supplier).OrderBy(g => g.Name).ToListAsync();
        return View(goods);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Suppliers = await _db.Suppliers.OrderBy(s => s.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MedicalGood good)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Suppliers = await _db.Suppliers.OrderBy(s => s.Name).ToListAsync();
            return View(good);
        }

        good.RegisteredByUserId = _userManager.GetUserId(User)!;
        good.DateReceived = DateTime.UtcNow;
        _db.MedicalGoods.Add(good);
        await _db.SaveChangesAsync(); // generates good.Id

        _db.StockTransactions.Add(new StockTransaction
        {
            ItemType = StockItemType.MedicalGood,
            ItemId = good.Id,
            TransactionType = StockTransactionType.StockIn,
            Quantity = good.QuantityInStock,
            ReferenceNote = "Initial registration",
            StaffId = good.RegisteredByUserId,
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = $"{good.Name} registered and available to Pharmacy.";
        return RedirectToAction(nameof(Index));
    }
}
