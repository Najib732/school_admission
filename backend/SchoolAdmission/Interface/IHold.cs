using SchoolAdmission.Common;
using SchoolAdmission.DTO;

namespace SchoolAdmission.Interface;

public interface IHold
{
    public Task<Result<HoldGetDto>> Apply(Guid classId, string studentId, TimeSpan holdDuration);
    public Task<Result<HoldGetDto>> GetById(Guid holdId);
    public Task<Result<HoldGetDto>> Confirm(Guid holdId, string studentId);
    public Task<Result<bool>> Cancel(Guid holdId, string studentId);
    public Task<Result<List<HoldGetDto>>> GetByStudentId(string studentId);
    public Task<int> ExpireOverdue();
}
