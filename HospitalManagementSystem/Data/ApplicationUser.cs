using Microsoft.AspNetCore.Identity;

namespace HospitalManagementSystem.Data;

// Extends the default Identity user with staff-specific fields.
// Role assignment (Receptionist, Triage, Doctor, LabTechnician, StoreMan,
// Pharmacist, Cashier, Admin) is handled via ASP.NET Identity's role system,
// not a field here — that keeps authorization consistent with [Authorize(Roles = "...")].
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
