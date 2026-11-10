namespace SchoolAdmission.Entities;

// One shift of a class, with its own seats ("Class 6 - Morning Shift").
public class Class
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int HeldSeats { get; set; }
    public int ConfirmedSeats { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDelete { get; set; }
}
