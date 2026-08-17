namespace HospitalManagementSystem.Models;

public class Visit
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public DateTime VisitDate { get; set; } = DateTime.UtcNow;
    public VisitStatus Status { get; set; } = VisitStatus.Open;
    // Which OPD department Triage routed this patient to — matches one of the
    // StaffRoles.OpdRoles values (e.g. "AdultOPD"). Null until Triage assigns it.
    public string? AssignedOpdRole { get; set; }

    public Triage? Triage { get; set; }
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
}
