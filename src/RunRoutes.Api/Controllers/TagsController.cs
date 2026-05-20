using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Tags.Dtos;

namespace RunRoutes.Api.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController(ITagRepository tagRepository, ITagService tagService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tags = await tagRepository.GetAllAsync();
        return Ok(tags.Select(t => new TagSummaryDto(t.Id, t.Name, t.Version)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateTagRequest request)
    {
        var result = await tagService.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), null, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTagRequest request)
    {
        var result = await tagService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] uint rowVersion)
    {
        await tagService.DeleteAsync(id, rowVersion);
        return NoContent();
    }
}
