namespace HospitalManagementSystem.Models;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;

    public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
    public ICollection<MedicalGood> MedicalGoods { get; set; } = new List<MedicalGood>();
}
