namespace HospitalManagementSystem.Models;

public class Patient
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string? Age { get; set; }
    public string? Wereda { get; set; }
    public string? Kebele { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
