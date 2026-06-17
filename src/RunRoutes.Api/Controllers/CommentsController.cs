using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunRoutes.Api.Extensions;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Commands.CreateComment;
using RunRoutes.Core.Courses.Commands.DeleteComment;
using RunRoutes.Core.Courses.Commands.UpdateComment;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Courses.Queries.GetComments;

namespace RunRoutes.Api.Controllers;

[ApiController]
[Route("api/courses/{courseId:guid}/comments")]
public class CommentsController(
    IQueryDispatcher queryDispatcher,
    ICommandDispatcher commandDispatcher
    ) : ControllerBase
{
    private readonly IQueryDispatcher _queryDispatcher = queryDispatcher;
    private readonly ICommandDispatcher _commandDispatcher = commandDispatcher;

    [HttpGet]
    public async Task<IActionResult> GetList(Guid courseId)
    {
        var result = await _queryDispatcher.SendAsync(new GetCommentsByCourseIdQuery(courseId));
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(Guid courseId, [FromBody] CreateCommentRequest request)
    {
        var userId = User.GetUserId();
        var command = new CreateCommentCommand(courseId, userId, request.Body);
        var id = await _commandDispatcher.SendAsync(command);
        var result = new CreateCommentResponse(new CreateCommentDto(id));
        return CreatedAtAction(nameof(GetList), new { courseId }, result);
    }

    [HttpPut("{commentId:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid courseId, Guid commentId, [FromBody] UpdateCommentRequest request)
    {
        var userId = User.GetUserId();
        var command = new UpdateCommentCommand(courseId, commentId, userId, request.Body);
        var id = await _commandDispatcher.SendAsync(command);
        var result = new UpdateCommentResponse(new UpdateCommentDto(id));
        return Ok(result);
    }

    [HttpDelete("{commentId:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid courseId, Guid commentId)
    {
        var userId = User.GetUserId();
        var command = new DeleteCommentCommand(courseId, commentId, userId);
        await _commandDispatcher.SendAsync(command);
        return NoContent();
    }
}
