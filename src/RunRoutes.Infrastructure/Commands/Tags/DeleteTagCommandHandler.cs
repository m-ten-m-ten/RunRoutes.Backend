using RunRoutes.Core.Common;
using RunRoutes.Core.Common.Commands;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Tags.Commands.DeleteTag;

namespace RunRoutes.Infrastructure.Commands.Tags;

public class DeleteTagCommandHandler(ITagRepository tagRepository) : ICommandHandler<DeleteTagCommand, Unit>
{
    private readonly ITagRepository _tagRepository = tagRepository;

    public async Task<Unit> HandleAsync(
        DeleteTagCommand command,
        CancellationToken cancellationToken
    )
    {
        var tag = await _tagRepository.GetByIdForUpdateAsync(command.Id)
            ?? throw new NotFoundException("タグが見つかりません");

        if (await _tagRepository.HasCoursesAsync(command.Id))
            throw new ConflictException("このタグは既存コースで使用中のため削除できません", ErrorCodes.TagInUse);

        await _tagRepository.DeleteWithConcurrencyCheckAsync(tag, command.RowVersion);
        return Unit.Value;
    }
}