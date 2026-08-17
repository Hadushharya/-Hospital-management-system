namespace HospitalManagementSystem.Models;

// Single-row table — hospital-wide info used on receipts/reports.
// Editable by Admin; there's always exactly one row (Id = 1).
public class HospitalSettings
{
    public int Id { get; set; }
    public string Name { get; set; } = "Hospital Management System";
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string TinNumber { get; set; } = string.Empty;
}