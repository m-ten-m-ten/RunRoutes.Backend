using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Tags.Commands.CreateTag;
using RunRoutes.Core.Tags.Dtos;

namespace RunRoutes.Infrastructure.Commands.Tags;

public class CreateTagCommandHandler(ITagRepository tagRepository) : ICommandHandler<CreateTagCommand, TagSummaryDto>
{
    private readonly ITagRepository _tagRepository = tagRepository;

    public async Task<TagSummaryDto> HandleAsync(
        CreateTagCommand command,
        CancellationToken cancellationToken
    )
    {
        var tag = Tag.Create(command.Name);

        if (await _tagRepository.ExistsByNameAsync(tag.Name))
            throw new ConflictException("同名のタグが既に存在します", ErrorCodes.TagNameDuplicate);

        await _tagRepository.AddAsync(tag);

        return new(tag.Id, tag.Name, tag.Version);
    }
}