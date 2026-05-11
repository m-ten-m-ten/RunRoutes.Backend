using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunRoutes.Api.Extensions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Dtos;

namespace RunRoutes.Api.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(ICourseService courseService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GetCoursesQuery query)
    {
        Guid? currentUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        var result = await courseService.GetListAsync(query, currentUserId);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        Guid? currentUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        var result = await courseService.GetByIdAsync(id, currentUserId);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest request)
    {
        var userId = User.GetUserId();
        var result = await courseService.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = result.Course.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseRequest request)
    {
        var userId = User.GetUserId();
        var result = await courseService.UpdateAsync(id, request, userId);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();
        await courseService.DeleteAsync(id, userId);
        return NoContent();
    }
}
