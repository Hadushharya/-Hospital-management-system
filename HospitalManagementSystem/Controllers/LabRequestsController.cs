
//using HospitalManagementSystem.Data;
//using HospitalManagementSystem.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace HospitalManagementSystem.Controllers;

//[Authorize(Roles = StaffRoles.LabTechnician + "," + StaffRoles.Admin)]
//public class LabRequestsController : Controller
//{
//    private readonly ApplicationDbContext _db;
//    private readonly UserManager<ApplicationUser> _userManager;

//    public LabRequestsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
//    {
//        _db = db;
//        _userManager = userManager;
//    }

//    // Requests that are paid for and waiting for a technician to run the test.
//    public async Task<IActionResult> Queue()
//    {
//        var queue = await _db.LabRequests
//            .Include(lr => lr.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
//            .Include(lr => lr.Invoice)
//            .Where(lr => lr.Status == LabRequestStatus.PaymentConfirmed)
//            .OrderBy(lr => lr.RequestedAt)
//            .ToListAsync();
//        return View(queue);
//    }

//    public async Task<IActionResult> EnterResult(int id)
//    {
//        var request = await _db.LabRequests
//            .Include(lr => lr.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
//            .FirstOrDefaultAsync(lr => lr.Id == id);
//        if (request is null || request.Status != LabRequestStatus.PaymentConfirmed) return NotFound();

//        ViewBag.LabRequest = request;
//        return View(new LabResult { LabRequestId = id });
//    }

//    // Entering a result no longer marks the request Completed. It moves to
//    // ResultEntered so the technician can review before deliberately sending it.
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> EnterResult(int labRequestId, string resultValue, string? notes)
//    {
//        var request = await _db.LabRequests.FindAsync(labRequestId);
//        if (request is null || request.Status != LabRequestStatus.PaymentConfirmed) return NotFound();

//        var result = new LabResult
//        {
//            LabRequestId = labRequestId,
//            ResultValue = resultValue,
//            Notes = notes ?? string.Empty,
//            TechnicianId = _userManager.GetUserId(User)!,
//            ResultDate = DateTime.UtcNow
//        };
//        _db.LabResults.Add(result);

//        // FIX: this was setting Completed, which skipped the review step entirely
//        // and made SendToDoctor's status check below impossible to satisfy.
//        request.Status = LabRequestStatus.ResultEntered;
//        request.ResultEnteredAt = DateTime.UtcNow;

//        await _db.SaveChangesAsync();
//        TempData["Success"] = "Result saved. Review it below and send it to the doctor when ready.";
//        return RedirectToAction(nameof(ReadyToSend));
//    }

//    // Results entered but not yet sent — the technician's review-and-send queue.
//    public async Task<IActionResult> ReadyToSend()
//    {
//        var queue = await _db.LabRequests
//            .Include(lr => lr.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
//            .Include(lr => lr.LabResult)
//            // FIX: was filtering on Completed, which is now the "already sent" state.
//            // This queue should show ResultEntered (saved, not yet sent).
//            .Where(lr => lr.Status == LabRequestStatus.ResultEntered)
//            .OrderBy(lr => lr.ResultEnteredAt)
//            .ToListAsync();
//        return View(queue);
//    }

//    // The deliberate "send" step — this is what actually hands the result to the doctor.
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> SendToDoctor(int id)
//    {
//        var request = await _db.LabRequests.FindAsync(id);
//        // FIX: was checking for Requested, a status this request never has at this
//        // point in the flow. It should be checking for ResultEntered — the state
//        // EnterResult now correctly puts it in.
//        if (request is null || request.Status != LabRequestStatus.ResultEntered) return NotFound();

//        request.Status = LabRequestStatus.Completed;
//        request.SentAt = DateTime.UtcNow;


//        // NEW: flag the consultation so the doctor sees a notification and is
//        // led toward writing a prescription based on this result.
//        request.Consultation.HasNewLabResult = true;
//        request.Consultation.LastLabResultAt = DateTime.UtcNow;

//        await _db.SaveChangesAsync();
//        TempData["Success"] = "Result sent to the doctor.";
//        return RedirectToAction(nameof(ReadyToSend));
//    }
//}
using HospitalManagementSystem.Data;
using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Controllers;

[Authorize(Roles = StaffRoles.LabTechnician + "," + StaffRoles.Admin)]
public class LabRequestsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public LabRequestsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Requests that are paid for and waiting for a technician to run the test.
    public async Task<IActionResult> Queue()
    {
        var queue = await _db.LabRequests
            .Include(lr => lr.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
            .Include(lr => lr.Invoice)
            .Where(lr => lr.Status == LabRequestStatus.PaymentConfirmed)
            .OrderBy(lr => lr.RequestedAt)
            .ToListAsync();
        return View(queue);
    }

    public async Task<IActionResult> EnterResult(int id)
    {
        var request = await _db.LabRequests
            .Include(lr => lr.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
            .FirstOrDefaultAsync(lr => lr.Id == id);
        if (request is null || request.Status != LabRequestStatus.PaymentConfirmed) return NotFound();

        ViewBag.LabRequest = request;
        return View(new LabResult { LabRequestId = id });
    }

    // Entering a result no longer marks the request Completed. It moves to
    // ResultEntered so the technician can review before deliberately sending it.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnterResult(int labRequestId, string resultValue, string? notes)
    {
        var request = await _db.LabRequests.FindAsync(labRequestId);
        if (request is null || request.Status != LabRequestStatus.PaymentConfirmed) return NotFound();

        var result = new LabResult
        {
            LabRequestId = labRequestId,
            ResultValue = resultValue,
            Notes = notes ?? string.Empty,
            TechnicianId = _userManager.GetUserId(User)!,
            ResultDate = DateTime.UtcNow
        };
        _db.LabResults.Add(result);

        request.Status = LabRequestStatus.ResultEntered;
        request.ResultEnteredAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Result saved. Review it below and send it to the doctor when ready.";
        return RedirectToAction(nameof(ReadyToSend));
    }

    // Results entered but not yet sent — the technician's review-and-send queue.
    public async Task<IActionResult> ReadyToSend()
    {
        var queue = await _db.LabRequests
            .Include(lr => lr.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
            .Include(lr => lr.LabResult)
            .Where(lr => lr.Status == LabRequestStatus.ResultEntered)
            .OrderBy(lr => lr.ResultEnteredAt)
            .ToListAsync();
        return View(queue);
    }

    // The deliberate "send" step — this is what actually hands the result to the doctor,
    // and flags the consultation so the doctor gets a notification.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendToDoctor(int id)
    {
        // FIX: FindAsync does not run .Include() — it only ever looks up LabRequest
        // itself, so request.Consultation came back null and blew up on the line
        // below. Must use a tracked query with Include instead.
        var request = await _db.LabRequests
            .Include(lr => lr.Consultation)
            .FirstOrDefaultAsync(lr => lr.Id == id);
        if (request is null || request.Status != LabRequestStatus.ResultEntered) return NotFound();

        request.Status = LabRequestStatus.Completed;
        request.SentAt = DateTime.UtcNow;

        request.Consultation.HasNewLabResult = true;
        request.Consultation.LastLabResultAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Result sent to the doctor.";
        return RedirectToAction(nameof(ReadyToSend));
    }
}









//using HospitalManagementSystem.Data;
//using HospitalManagementSystem.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace HospitalManagementSystem.Controllers;

//[Authorize(Roles = StaffRoles.LabTechnician + "," + StaffRoles.Admin)]
//public class LabRequestsController : Controller
//{
//    private readonly ApplicationDbContext _db;
//    private readonly UserManager<ApplicationUser> _userManager;

//    public LabRequestsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
//    {
//        _db = db;
//        _userManager = userManager;
//    }

//    // Requests that are paid for and waiting for a technician to run the test.
//    public async Task<IActionResult> Queue()
//    {
//        var queue = await _db.LabRequests
//            .Include(lr => lr.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
//            .Include(lr => lr.Invoice)
//            .Where(lr => lr.Status == LabRequestStatus.PaymentConfirmed)
//            .OrderBy(lr => lr.RequestedAt)
//            .ToListAsync();
//        return View(queue);
//    }

//    public async Task<IActionResult> EnterResult(int id)
//    {
//        var request = await _db.LabRequests
//            .Include(lr => lr.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
//            .FirstOrDefaultAsync(lr => lr.Id == id);
//        if (request is null || request.Status != LabRequestStatus.PaymentConfirmed) return NotFound();

//        ViewBag.LabRequest = request;
//        return View(new LabResult { LabRequestId = id });
//    }

//    // Entering a result no longer marks the request Completed. It moves to
//    // ResultEntered so the technician can review before deliberately sending it.
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> EnterResult(int labRequestId, string resultValue, string? notes)
//    {
//        var request = await _db.LabRequests.FindAsync(labRequestId);
//        if (request is null || request.Status != LabRequestStatus.PaymentConfirmed) return NotFound();

//        var result = new LabResult
//        {
//            LabRequestId = labRequestId,
//            ResultValue = resultValue,
//            Notes = notes ?? string.Empty,
//            TechnicianId = _userManager.GetUserId(User)!,
//            ResultDate = DateTime.UtcNow
//        };
//        _db.LabResults.Add(result);

//        request.Status = LabRequestStatus.Completed;
//        request.ResultEnteredAt = DateTime.UtcNow; // ASSUMPTION: new field, see model note below

//        await _db.SaveChangesAsync();
//        TempData["Success"] = "Result saved. Review it below and send it to the doctor when ready.";
//        return RedirectToAction(nameof(ReadyToSend));
//    }

//    // Results entered but not yet sent — the technician's review-and-send queue.
//    public async Task<IActionResult> ReadyToSend()
//    {
//        var queue = await _db.LabRequests
//            .Include(lr => lr.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
//            .Include(lr => lr.LabResult)
//            .Where(lr => lr.Status == LabRequestStatus.Completed)
//            .OrderBy(lr => lr.ResultEnteredAt)
//            .ToListAsync();
//        return View(queue);
//    }

//    // The deliberate "send" step — this is what actually hands the result to the doctor.
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> SendToDoctor(int id)
//    {
//        var request = await _db.LabRequests.FindAsync(id);
//        if (request is null || request.Status != LabRequestStatus.Requested) return NotFound();

//        request.Status = LabRequestStatus.Completed;
//        request.SentAt = DateTime.UtcNow; // ASSUMPTION: new field, see model note below

//        await _db.SaveChangesAsync();
//        TempData["Success"] = "Result sent to the doctor.";
//        return RedirectToAction(nameof(ReadyToSend));
//    }
//}


//using HospitalManagementSystem.Data;
//using HospitalManagementSystem.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace HospitalManagementSystem.Controllers;

//[Authorize(Roles = StaffRoles.LabTechnician + "," + StaffRoles.Admin)]
//public class LabRequestsController : Controller
//{
//    private readonly ApplicationDbContext _db;
//    private readonly UserManager<ApplicationUser> _userManager;

//    public LabRequestsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
//    {
//        _db = db;
//        _userManager = userManager;
//    }

//    public async Task<IActionResult> Queue()
//    {
//        var queue = await _db.LabRequests
//            .Include(lr => lr.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
//            .Include(lr => lr.Invoice)
//            .Where(lr => lr.Status == LabRequestStatus.PaymentConfirmed)
//            .OrderBy(lr => lr.RequestedAt)
//            .ToListAsync();
//        return View(queue);
//    }

//    public async Task<IActionResult> EnterResult(int id)
//    {
//        var request = await _db.LabRequests
//            .Include(lr => lr.Consultation).ThenInclude(c => c.Visit).ThenInclude(v => v.Patient)
//            .FirstOrDefaultAsync(lr => lr.Id == id);
//        if (request is null || request.Status != LabRequestStatus.PaymentConfirmed) return NotFound();

//        ViewBag.LabRequest = request;
//        return View(new LabResult { LabRequestId = id });
//    }

//    //[HttpPost]
//    //[ValidateAntiForgeryToken]
//    //public async Task<IActionResult> EnterResult(LabResult result)
//    //{
//    //    result.TechnicianId = _userManager.GetUserId(User)!;
//    //    result.ResultDate = DateTime.UtcNow;
//    //    _db.LabResults.Add(result);

//    //    var request = await _db.LabRequests.FindAsync(result.LabRequestId);
//    //    if (request is not null) request.Status = LabRequestStatus.Completed;

//    //    await _db.SaveChangesAsync();
//    //    TempData["Success"] = "Result entered. Doctor can now review it.";
//    //    return RedirectToAction(nameof(Queue));
//    //}
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> EnterResult(int labRequestId, string resultValue, string? notes)
//    {
//        var result = new LabResult
//        {
//            LabRequestId = labRequestId,
//            ResultValue = resultValue,
//            Notes = notes ?? string.Empty,
//            TechnicianId = _userManager.GetUserId(User)!,
//            ResultDate = DateTime.UtcNow
//        };
//        _db.LabResults.Add(result);

//        var request = await _db.LabRequests.FindAsync(labRequestId);
//        if (request is not null) request.Status = LabRequestStatus.Completed;

//        await _db.SaveChangesAsync();
//        TempData["Success"] = "Result entered. Doctor can now review it.";
//        return RedirectToAction(nameof(Queue));
//    }
//}
