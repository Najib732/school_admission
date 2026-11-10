using SchoolAdmission.Common;
using SchoolAdmission.DTO;
using SchoolAdmission.Interface;

namespace SchoolAdmission.Services;

public class HoldService
{

    private readonly IHold obj;
    private readonly HoldSettings _settings;

    public HoldService(IHold obj, HoldSettings settings)
    {
        this.obj = obj;
        _settings = settings;
    }

    public async Task<Result<HoldGetDto>> Apply(Guid classId, string? studentId)
    {
        var error = ValidateStudentId(studentId);
        if (error != null)
        {
            return Result<HoldGetDto>.Fail(error);
        }

        return await obj.Apply(classId, studentId!.Trim(), _settings.HoldDuration);
    }

    public async Task<Result<HoldGetDto>> GetById(Guid holdId)
    {
        return await obj.GetById(holdId);
    }

    public async Task<Result<HoldGetDto>> Confirm(Guid holdId, string? studentId)
    {
        var error = ValidateStudentId(studentId);
        if (error != null)
        {
            return Result<HoldGetDto>.Fail(error);
        }

        var sid = studentId!.Trim();

        // business rule: only the student's own application; someone else's looks like it doesn't exist
        var hold = await obj.GetById(holdId);
        if (!hold.Success || hold.Data!.StudentId != sid)
        {
            return Result<HoldGetDto>.NotFound($"Application with id {holdId} not found");
        }

        return await obj.Confirm(holdId, sid);
    }

    public async Task<Result<bool>> Cancel(Guid holdId, string? studentId)
    {
        var error = ValidateStudentId(studentId);
        if (error != null)
        {
            return Result<bool>.Fail(error);
        }

        var sid = studentId!.Trim();

        // business rule: only the student's own application; someone else's looks like it doesn't exist
        var hold = await obj.GetById(holdId);
        if (!hold.Success || hold.Data!.StudentId != sid)
        {
            return Result<bool>.NotFound($"Application with id {holdId} not found");
        }

        return await obj.Cancel(holdId, sid);
    }

    public async Task<Result<List<HoldGetDto>>> GetMine(string? studentId)
    {
        var error = ValidateStudentId(studentId);
        if (error != null)
        {
            return Result<List<HoldGetDto>>.Fail(error);
        }

        return await obj.GetByStudentId(studentId!.Trim());
    }

    // The student is the 17-digit birth registration number sent in the X-Student-Id header.
    private static string? ValidateStudentId(string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return "The birth registration number is missing (X-Student-Id header).";
        }

        var value = studentId.Trim();
        if (value.Length != 17 || !value.All(char.IsAsciiDigit))
        {
            return "The birth registration number must be exactly 17 digits.";
        }

        return null;
    }
}
