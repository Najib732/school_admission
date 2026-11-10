using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Common;
using SchoolAdmission.DTO;
using SchoolAdmission.Services;

namespace SchoolAdmission.Controllers;

[ApiController]
[Route("classes")]
public class ClassController : ControllerBase
{
    private readonly ClassService _service;

    public ClassController(ClassService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ClassGetDto>> Add(ClassAddDto dto)
    {
        var result = await _service.Add(dto);
        return this.ToCreatedResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<ClassGetDto>>> GetAll()
    {
        var result = await _service.GetAll();
        return this.ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClassGetDto>> GetById(Guid id)
    {
        var result = await _service.GetById(id);
        return this.ToActionResult(result);
    }
}
