using SchoolAdmission.Entities;

namespace SchoolAdmission.DTO;

public class HoldGetDto
{
    public Guid HoldId { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public HoldStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
