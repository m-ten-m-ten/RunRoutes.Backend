using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Tags.Commands.UpdateTag;
using RunRoutes.Core.Tags.Dtos;

namespace RunRoutes.Infrastructure.Commands.Tags;

public class UpdateTagCommandHandler(ITagRepository tagRepository) : ICommandHandler<UpdateTagCommand, TagSummaryDto>
{
    private readonly ITagRepository _tagRepository = tagRepository;

    public async Task<TagSummaryDto> HandleAsync(
        UpdateTagCommand command,
        CancellationToken cancellationToken
    )
    {
        var tag = await _tagRepository.GetByIdForUpdateAsync(command.Id)
    ?? throw new NotFoundException("タグが見つかりません");

        var normalizedName = Tag.NormalizeNameForService(command.Name);

        if (!string.Equals(tag.Name, normalizedName, StringComparison.Ordinal))
        {
            if (await _tagRepository.ExistsByNameAsync(normalizedName, command.Id))
                throw new ConflictException("同名のタグが既に存在します", ErrorCodes.TagNameDuplicate);

            tag.Rename(normalizedName);
        }

        await _tagRepository.UpdateWithConcurrencyCheckAsync(tag, command.RowVersion);

        return new(tag.Id, tag.Name, tag.Version);
    }
}