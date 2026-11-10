using SchoolAdmission.Common;
using SchoolAdmission.DTO;

namespace SchoolAdmission.Interface;

public interface IClass
{
    public Task<Result<ClassGetDto>> Add(ClassAddDto dto);
    public Task<Result<List<ClassGetDto>>> GetAll();
    public Task<Result<ClassGetDto>> GetById(Guid id);
}
