using HospitalManagementSystem.Data;

namespace HospitalManagementSystem.Models;

public class Consultation
{
    public int Id { get; set; }

    public int VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    public string DoctorId { get; set; } = string.Empty;
    public ApplicationUser Doctor { get; set; } = null!;

    public string Diagnosis { get; set; } = string.Empty;
    public string? Notes { get; set; } 
    public DateTime ConsultationDate { get; set; } = DateTime.UtcNow;
    // NEW: flips true when a lab result is sent to this consultation's doctor;
    // cleared when the doctor opens Manage. Drives ResultsQueue / notifications.
    public bool HasNewLabResult { get; set; } = false;
    public DateTime? LastLabResultAt { get; set; }

    public ICollection<LabRequest> LabRequests { get; set; } = new List<LabRequest>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
