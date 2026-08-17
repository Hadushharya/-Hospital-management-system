using HospitalManagementSystem.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace HospitalManagementSystem.Models;

public class Medicine
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string BatchNo { get; set; } = string.Empty;
    public DateOnly ExpiryDate { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public string DosageForm { get; set; } = string.Empty;
    public bool ControlledSubstance { get; set; }

    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public string RegisteredByUserId { get; set; } = string.Empty;
    [ValidateNever]
    public ApplicationUser RegisteredByUser { get; set; } = null!;

    public DateTime DateReceived { get; set; } = DateTime.UtcNow;

    public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}
