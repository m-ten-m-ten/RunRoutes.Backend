using RunRoutes.Core.DTOs.Comments;
using RunRoutes.Core.DTOs.Common;
using RunRoutes.Core.Entities;
using RunRoutes.Core.Exceptions;
using RunRoutes.Core.Interfaces.Repositories;
using RunRoutes.Core.Interfaces.Services;

namespace RunRoutes.Core.Services;

public class CommentService(ICommentRepository commentRepository, ICourseRepository courseRepository) : ICommentService
{
    private readonly ICommentRepository _commentRepository = commentRepository;
    private readonly ICourseRepository _courseRepository = courseRepository;

    public async Task<GetCommentsResponse> GetByCourseIdAsync(Guid courseId)
    {
        var comments = await _commentRepository.GetByCourseIdAsync(courseId);
        return new GetCommentsResponse(comments.Select(ToCommentDto));
    }

    public async Task<CreateCommentResponse> CreateAsync(Guid courseId, CreateCommentRequest request, Guid userId)
    {
        if (!await _courseRepository.ExistsByIdAsync(courseId))
            throw new NotFoundException("コースが見つかりません");

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            UserId = userId,
            Body = request.Body,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _commentRepository.AddAsync(comment);
        return new CreateCommentResponse(ToCommentDto(comment));
    }

    public async Task<UpdateCommentResponse> UpdateAsync(Guid courseId, Guid commentId, UpdateCommentRequest request, Guid userId)
    {
        var comment = await _commentRepository.GetByIdForUpdateAsync(commentId)
            ?? throw new NotFoundException("コメントが見つかりません");

        if (comment.CourseId != courseId)
            throw new NotFoundException("コメントが見つかりません");

        if (comment.UserId != userId)
            throw new ForbiddenException("このコメントを編集する権限がありません");

        comment.Body = request.Body;
        comment.UpdatedAt = DateTime.UtcNow;

        await _commentRepository.UpdateAsync(comment);
        return new UpdateCommentResponse(ToCommentDto(comment));
    }

    public async Task DeleteAsync(Guid courseId, Guid commentId, Guid userId)
    {
        var comment = await _commentRepository.GetByIdForUpdateAsync(commentId)
            ?? throw new NotFoundException("コメントが見つかりません");

        if (comment.CourseId != courseId)
            throw new NotFoundException("コメントが見つかりません");

        if (comment.UserId != userId)
            throw new ForbiddenException("このコメントを削除する権限がありません");

        await _commentRepository.DeleteAsync(comment);
    }

    private static CommentDto ToCommentDto(Comment c)
    {
        var isEdited = c.UpdatedAt > c.CreatedAt.AddSeconds(1);
        UserDto user = c.User is null
            ? new UserDto(c.UserId, string.Empty, string.Empty, UserRole.User.ToString(), DateTime.MinValue)
            : new UserDto(c.User.Id, c.User.Email, c.User.Username, c.User.Role.ToString(), c.User.CreatedAt);
        return new CommentDto(c.Id, c.Body, user, c.CreatedAt, c.UpdatedAt, isEdited);
    }
}
