namespace HospitalManagementSystem.Models;

public class Prescription
{
    public int Id { get; set; }

    public int ConsultationId { get; set; }
    public Consultation Consultation { get; set; } = null!;

    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Pending;

    // Free-text instructions from the doctor — lets a prescription go straight to
    // Pharmacy even when no items exist yet in the Medicine catalog, or alongside items.
    public string? Notes { get; set; }

    // Optional link to the lab request this prescription was written in
    // response to. Null means the doctor prescribed directly, with no
    // preceding lab test.
    public int? BasedOnLabRequestId { get; set; }
    public LabRequest? BasedOnLabRequest { get; set; }

    public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
    public Payment? Payment { get; set; }
}