using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Common;
using SchoolAdmission.DTO;
using SchoolAdmission.Services;

namespace SchoolAdmission.Controllers;

[ApiController]
public class HoldController : ControllerBase
{
    private const string StudentHeader = "X-Student-Id";

    private readonly HoldService _service;

    public HoldController(HoldService service)
    {
        _service = service;
    }

    [HttpPost("classes/{classId}/holds")]
    public async Task<ActionResult<HoldGetDto>> Apply(Guid classId, [FromHeader(Name = StudentHeader)] string? studentId)
    {
        var result = await _service.Apply(classId, studentId);
        return this.ToCreatedResult(result);
    }

    [HttpGet("holds/{holdId}")]
    public async Task<ActionResult<HoldGetDto>> GetById(Guid holdId)
    {
        var result = await _service.GetById(holdId);
        return this.ToActionResult(result);
    }

    [HttpPost("holds/{holdId}/confirm")]
    public async Task<ActionResult<HoldGetDto>> Confirm(Guid holdId, [FromHeader(Name = StudentHeader)] string? studentId)
    {
        var result = await _service.Confirm(holdId, studentId);
        return this.ToActionResult(result);
    }

    [HttpDelete("holds/{holdId}")]
    public async Task<ActionResult> Cancel(Guid holdId, [FromHeader(Name = StudentHeader)] string? studentId)
    {
        var result = await _service.Cancel(holdId, studentId);
        return this.ToNoContentResult(result);
    }

    [HttpGet("me/holds")]
    public async Task<ActionResult<List<HoldGetDto>>> GetMine([FromHeader(Name = StudentHeader)] string? studentId)
    {
        var result = await _service.GetMine(studentId);
        return this.ToActionResult(result);
    }
}
