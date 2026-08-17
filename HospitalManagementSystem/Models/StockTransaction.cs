using HospitalManagementSystem.Data;

namespace HospitalManagementSystem.Models;

// Polymorphic by design: ItemType + ItemId together point at either a Medicine
// or a MedicalGood row. EF Core can't enforce a real FK across two possible
// tables, so this is intentionally a "soft" reference resolved in application code.
public class StockTransaction
{
    public int Id { get; set; }

    public StockItemType ItemType { get; set; }
    public int ItemId { get; set; }

    public StockTransactionType TransactionType { get; set; }
    public int Quantity { get; set; }

    public string? ReferenceNote { get; set; }

    public string StaffId { get; set; } = string.Empty;
    public ApplicationUser Staff { get; set; } = null!;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
