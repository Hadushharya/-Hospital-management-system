using HospitalManagementSystem.Data;

namespace HospitalManagementSystem.Models;

public class Payment
{
    public int Id { get; set; }

    public int PrescriptionId { get; set; }
    public Prescription Prescription { get; set; } = null!;

    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    // Auto-generated after Id is known (see CollectPayment) — e.g. "RCPT-000123".
    // Never typed by hand, so it can't collide or be duplicated.
    public string ReceiptNo { get; set; } = string.Empty;

    public string CashierId { get; set; } = string.Empty;
    public ApplicationUser Cashier { get; set; } = null!;

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}