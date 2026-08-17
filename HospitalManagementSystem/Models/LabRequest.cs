namespace HospitalManagementSystem.Models;

public class LabRequest
{
    public int Id { get; set; }

    public int ConsultationId { get; set; }
    public Consultation Consultation { get; set; } = null!;

    public string TestName { get; set; } = string.Empty;
    public LabRequestStatus Status { get; set; } = LabRequestStatus.Requested;

    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;


    // NEW: when the technician saved a result (status -> ResultEntered)
    public DateTime? ResultEnteredAt { get; set; }

    // NEW: when the technician deliberately sent it on (status -> Completed)
    public DateTime? SentAt { get; set; }

    // NEW: when the doctor opened the chart and saw the sent result
    public DateTime? DoctorReviewedAt { get; set; }

    public LabResult? LabResult { get; set; }
}
