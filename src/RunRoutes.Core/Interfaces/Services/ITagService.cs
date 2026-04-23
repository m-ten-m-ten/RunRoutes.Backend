using RunRoutes.Core.DTOs.Tags;

namespace RunRoutes.Core.Interfaces.Services;

public interface ITagService
{
    Task<CreateTagResponse> CreateAsync(CreateTagRequest request);
    Task<UpdateTagResponse> UpdateAsync(Guid id, UpdateTagRequest request);
    Task DeleteAsync(Guid id, uint rowVersion);
}
