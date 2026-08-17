namespace HospitalManagementSystem.Models;

public class Triage
{
    public int Id { get; set; }

    public int VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    public string Vitals { get; set; } = string.Empty;
    public string? Notes { get; set; } 
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
