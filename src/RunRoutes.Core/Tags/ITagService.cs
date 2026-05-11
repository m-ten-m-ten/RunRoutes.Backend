using RunRoutes.Core.Tags.Dtos;

namespace RunRoutes.Core.Tags;

public interface ITagService
{
    Task<CreateTagResponse> CreateAsync(CreateTagRequest request);
    Task<UpdateTagResponse> UpdateAsync(Guid id, UpdateTagRequest request);
    Task DeleteAsync(Guid id, uint rowVersion);
}
