using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunRoutes.Api.Extensions;
using RunRoutes.Core.DTOs.Comments;
using RunRoutes.Core.Interfaces.Services;

namespace RunRoutes.Api.Controllers;

[ApiController]
[Route("api/courses/{courseId:guid}/comments")]
public class CommentsController(ICommentService commentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(Guid courseId)
    {
        var result = await commentService.GetByCourseIdAsync(courseId);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(Guid courseId, [FromBody] CreateCommentRequest request)
    {
        var userId = User.GetUserId();
        var result = await commentService.CreateAsync(courseId, request, userId);
        return CreatedAtAction(nameof(GetList), new { courseId }, result);
    }

    [HttpPut("{commentId:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid courseId, Guid commentId, [FromBody] UpdateCommentRequest request)
    {
        var userId = User.GetUserId();
        var result = await commentService.UpdateAsync(courseId, commentId, request, userId);
        return Ok(result);
    }

    [HttpDelete("{commentId:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid courseId, Guid commentId)
    {
        var userId = User.GetUserId();
        await commentService.DeleteAsync(courseId, commentId, userId);
        return NoContent();
    }
}
