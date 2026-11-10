using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.DTO;

public class ClassAddDto
{
    [Required(ErrorMessage = "Class Name Is Required")]
    [StringLength(200, ErrorMessage = "Class Name Must Be At Most 200 Characters")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Total Seats Must Be Between 1 And 100000")]
    public int TotalSeats { get; set; }
}

public class ClassGetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int HeldSeats { get; set; }
    public int ConfirmedSeats { get; set; }
    public int AvailableSeats { get; set; }
}
