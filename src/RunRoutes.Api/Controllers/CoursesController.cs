using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunRoutes.Api.Extensions;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Commands.CreateCourse;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Courses.Queries.GetCourseById;

namespace RunRoutes.Api.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(
    ICourseService courseService,
    IQueryDispatcher queryDispatcher,
    ICommandDispatcher commandDispatcher
    ) : ControllerBase
{
    private readonly IQueryDispatcher _queryDispatcher = queryDispatcher;
    private readonly ICommandDispatcher _commandDispatcher = commandDispatcher;

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
        var dto = await _queryDispatcher.SendAsync(new GetCourseByIdQuery(id, currentUserId));
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest request)
    {
        var userId = User.GetUserId();
        var command = new CreateCourseCommand(request.Title, request.Description, request.Difficulty, request.IsPublic, request.Route, request.GpxXml, request.TagIds, userId);
        var courseId = await _commandDispatcher.SendAsync(command);
        var result = new CreateCourseResponse(new CreateCourseDto(courseId));
        return CreatedAtAction(nameof(GetById), new { id = courseId }, result);
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
