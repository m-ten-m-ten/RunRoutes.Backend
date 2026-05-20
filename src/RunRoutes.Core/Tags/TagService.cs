using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Tags.Dtos;

namespace RunRoutes.Core.Tags;

public class TagService(ITagRepository tagRepository) : ITagService
{
    private readonly ITagRepository _tagRepository = tagRepository;

    public async Task<CreateTagResponse> CreateAsync(CreateTagRequest request)
    {
        var tag = Tag.Create(request.Name);

        if (await _tagRepository.ExistsByNameAsync(tag.Name))
            throw new ConflictException("同名のタグが既に存在します", ErrorCodes.TagNameDuplicate);

        await _tagRepository.AddAsync(tag);

        return new CreateTagResponse(ToSummaryDto(tag));
    }

    public async Task<UpdateTagResponse> UpdateAsync(Guid id, UpdateTagRequest request)
    {
        var tag = await _tagRepository.GetByIdForUpdateAsync(id)
            ?? throw new NotFoundException("タグが見つかりません");

        var normalizedName = Tag.NormalizeNameForService(request.Name);

        if (!string.Equals(tag.Name, normalizedName, StringComparison.Ordinal))
        {
            if (await _tagRepository.ExistsByNameAsync(normalizedName, id))
                throw new ConflictException("同名のタグが既に存在します", ErrorCodes.TagNameDuplicate);

            tag.Rename(normalizedName);
        }

        await _tagRepository.UpdateWithConcurrencyCheckAsync(tag, request.RowVersion);

        return new UpdateTagResponse(ToSummaryDto(tag));
    }

    public async Task DeleteAsync(Guid id, uint rowVersion)
    {
        var tag = await _tagRepository.GetByIdForUpdateAsync(id)
            ?? throw new NotFoundException("タグが見つかりません");

        if (await _tagRepository.HasCoursesAsync(id))
            throw new ConflictException("このタグは既存コースで使用中のため削除できません", ErrorCodes.TagInUse);

        await _tagRepository.DeleteWithConcurrencyCheckAsync(tag, rowVersion);
    }

    private static TagSummaryDto ToSummaryDto(Tag tag) =>
        new(tag.Id, tag.Name, tag.Version);
}
