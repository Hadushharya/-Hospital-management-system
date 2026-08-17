using HospitalManagementSystem.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace HospitalManagementSystem.Models;

public class MedicalGood
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string BatchNo { get; set; } = string.Empty;
    public DateOnly ExpiryDate { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;

    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public string RegisteredByUserId { get; set; } = string.Empty;
    [ValidateNever]
    public ApplicationUser RegisteredByUser { get; set; } = null!;

    public DateTime DateReceived { get; set; } = DateTime.UtcNow;
}
