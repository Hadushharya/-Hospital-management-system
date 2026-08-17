using HospitalManagementSystem.Data;

namespace HospitalManagementSystem.Models;

public class Invoice
{
    public int Id { get; set; }

    public int VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    // Which fee-schedule entry this invoice was generated from (see FeeSetting).
    // Nullable because older/manual invoices might not reference one.
    public int? FeeSettingId { get; set; }
    public FeeSetting? FeeSetting { get; set; }

    public FeeType FeeType { get; set; }
    public decimal Amount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    // Null until Reception actually collects the fee (set in InvoicesController.Collect)
    public string? CollectedByUserId { get; set; }
    public ApplicationUser? CollectedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public LabRequest? LabRequest { get; set; }
}
