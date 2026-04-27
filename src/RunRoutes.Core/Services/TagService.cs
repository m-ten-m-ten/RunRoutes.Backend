using RunRoutes.Core.DTOs.Tags;
using RunRoutes.Core.Entities;
using RunRoutes.Core.Exceptions;
using RunRoutes.Core.Interfaces.Repositories;
using RunRoutes.Core.Interfaces.Services;

namespace RunRoutes.Core.Services;

public class TagService(ITagRepository tagRepository) : ITagService
{
    private const int MaxNameLength = 50;

    private readonly ITagRepository _tagRepository = tagRepository;

    public async Task<CreateTagResponse> CreateAsync(CreateTagRequest request)
    {
        var name = NormalizeName(request.Name);

        if (await _tagRepository.ExistsByNameAsync(name))
            throw new ConflictException("同名のタグが既に存在します", ErrorCodes.TagNameDuplicate);

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow,
        };

        await _tagRepository.AddAsync(tag);

        return new CreateTagResponse(ToSummaryDto(tag));
    }

    public async Task<UpdateTagResponse> UpdateAsync(Guid id, UpdateTagRequest request)
    {
        var name = NormalizeName(request.Name);

        var tag = await _tagRepository.GetByIdForUpdateAsync(id)
            ?? throw new NotFoundException("タグが見つかりません");

        if (!string.Equals(tag.Name, name, StringComparison.Ordinal))
        {
            if (await _tagRepository.ExistsByNameAsync(name, id))
                throw new ConflictException("同名のタグが既に存在します", ErrorCodes.TagNameDuplicate);
            tag.Name = name;
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

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["name"] = ["名前は必須です"]
            });

        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["name"] = [$"名前は {MaxNameLength} 文字以内で入力してください"]
            });

        return trimmed;
    }

    private static TagSummaryDto ToSummaryDto(Tag tag) =>
        new(tag.Id, tag.Name, tag.Version);
}
