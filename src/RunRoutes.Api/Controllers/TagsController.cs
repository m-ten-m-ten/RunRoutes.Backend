using Microsoft.AspNetCore.Mvc;
using RunRoutes.Core.DTOs.Common;
using RunRoutes.Core.Interfaces.Repositories;

namespace RunRoutes.Api.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController(ITagRepository tagRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tags = await tagRepository.GetAllAsync();
        return Ok(tags.Select(t => new TagDto(t.Id, t.Name)));
    }
}
