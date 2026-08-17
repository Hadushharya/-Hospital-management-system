using HospitalManagementSystem.Data;

namespace HospitalManagementSystem.Models;

public class LabResult
{
    public int Id { get; set; }

    public int LabRequestId { get; set; }
    public LabRequest LabRequest { get; set; } = null!;

    public string ResultValue { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public string TechnicianId { get; set; } = string.Empty;
    public ApplicationUser Technician { get; set; } = null!;

    public DateTime ResultDate { get; set; } = DateTime.UtcNow;
}
