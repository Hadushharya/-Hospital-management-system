
using System.Diagnostics;
using HospitalManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagementSystem.Controllers;

public class HomeController : Controller
{
    // Landing page after login. Redirects each role straight to its own dashboard
    // instead of showing the generic hub of buttons.
    public IActionResult Index()
    {
        if (User.Identity is { IsAuthenticated: true })
        {
            if (User.IsInRole(StaffRoles.Admin)) return RedirectToAction("Admin", "Dashboard");
            if (User.IsInRole(StaffRoles.Receptionist)) return RedirectToAction("Reception", "Dashboard");
            if (User.IsInRole(StaffRoles.TriageNurse)) return RedirectToAction("Triage", "Dashboard");
            if (StaffRoles.OpdRoles.Any(r => User.IsInRole(r))) return RedirectToAction("OPD", "Dashboard");
            if (User.IsInRole(StaffRoles.LabTechnician)) return RedirectToAction("Lab", "Dashboard");
            if (User.IsInRole(StaffRoles.Cashier)) return RedirectToAction("Cashier", "Dashboard");
            if (User.IsInRole(StaffRoles.Pharmacist)) return RedirectToAction("Pharmacy", "Dashboard");
            // StoreMan (and any future role without its own dashboard yet) falls
            // through to the generic hub view below.
        }
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}


//using System.Diagnostics;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace HospitalManagementSystem.Controllers;

//public class HomeController : Controller
//{
//    // Landing dashboard — links shown are filtered by role in the view (User.IsInRole(...))
//    public IActionResult Index() => View();

//    [AllowAnonymous]
//    public IActionResult Error() => View();
//}
