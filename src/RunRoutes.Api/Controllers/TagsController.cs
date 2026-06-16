using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Tags.Queries.ListTags;
using RunRoutes.Core.Tags.Dtos;
using RunRoutes.Core.Tags.Commands.CreateTag;
using RunRoutes.Core.Tags.Commands.UpdateTag;
using RunRoutes.Core.Tags.Commands.DeleteTag;

namespace RunRoutes.Api.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController(
    IQueryDispatcher queryDispatcher,
    ICommandDispatcher commandDispatcher
    ) : ControllerBase
{

    private readonly IQueryDispatcher _queryDispatcher = queryDispatcher;
    private readonly ICommandDispatcher _commandDispatcher = commandDispatcher;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _queryDispatcher.SendAsync(new ListTagsQuery());
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateTagRequest request)
    {
        var command = new CreateTagCommand(request.Name);
        var result = await _commandDispatcher.SendAsync(command);
        return CreatedAtAction(nameof(GetAll), null, new CreateTagResponse(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTagRequest request)
    {
        var command = new UpdateTagCommand(id, request.Name, request.RowVersion);
        var result = await _commandDispatcher.SendAsync(command);
        return Ok(new UpdateTagResponse(result));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] uint rowVersion)
    {
        var command = new DeleteTagCommand(id, rowVersion);
        await _commandDispatcher.SendAsync(command);
        return NoContent();
    }
}
