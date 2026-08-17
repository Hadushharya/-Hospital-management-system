namespace HospitalManagementSystem.Models;

// Admin-managed fee schedule. Reception/OPD select from this list instead of
// typing a free-text amount — keeps pricing consistent and auditable.
// "Id" doubles as the payment order/code number you referred to.
public class FeeSetting
{
    public int Id { get; set; }
    public string PaymentName { get; set; } = string.Empty; // e.g. "Registration Card Fee"
    public FeeType FeeType { get; set; }                    // Registration, Consultation, Lab
    public decimal Amount { get; set; }
    public bool IsActive { get; set; } = true;
}
