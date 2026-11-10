using SchoolAdmission.Common;
using SchoolAdmission.DTO;
using SchoolAdmission.Interface;

namespace SchoolAdmission.Services;

public class ClassService
{

    private readonly IClass obj;

    public ClassService(IClass obj)
    {
        this.obj = obj;
    }

    public async Task<Result<ClassGetDto>> Add(ClassAddDto dto)
    {
        return await obj.Add(dto);
    }

    public async Task<Result<List<ClassGetDto>>> GetAll()
    {
        return await obj.GetAll();
    }

    public async Task<Result<ClassGetDto>> GetById(Guid id)
    {
        return await obj.GetById(id);
    }
}
