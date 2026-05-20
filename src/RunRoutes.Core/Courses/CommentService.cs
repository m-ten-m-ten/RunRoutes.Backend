using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Users;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Courses;

public class CommentService(ICourseRepository courseRepository) : ICommentService
{
    private readonly ICourseRepository _courseRepository = courseRepository;

    public async Task<GetCommentsResponse> GetByCourseIdAsync(Guid courseId)
    {
        var course = await _courseRepository.GetByIdAsync(courseId)
            ?? throw new NotFoundException("コースが見つかりません");
        return new GetCommentsResponse(course.Comments.Select(ToCommentDto));
    }

    public async Task<CreateCommentResponse> CreateAsync(Guid courseId, CreateCommentRequest request, Guid userId)
    {
        var course = await _courseRepository.GetByIdForUpdateAsync(courseId)
            ?? throw new NotFoundException("コースが見つかりません");
        var comment = course.AddComment(userId, request.Body);
        await _courseRepository.UpdateAsync(course);

        return new CreateCommentResponse(ToCommentDto(comment));
    }

    public async Task<UpdateCommentResponse> UpdateAsync(Guid courseId, Guid commentId, UpdateCommentRequest request, Guid userId)
    {
        var course = await _courseRepository.GetByIdForUpdateAsync(courseId)
            ?? throw new NotFoundException("コメントが見つかりません");

        var comment = course.EditComment(commentId, userId, request.Body);
        await _courseRepository.UpdateAsync(course);

        return new UpdateCommentResponse(ToCommentDto(comment));
    }

    public async Task DeleteAsync(Guid courseId, Guid commentId, Guid userId)
    {
        var course = await _courseRepository.GetByIdForUpdateAsync(courseId)
            ?? throw new NotFoundException("コメントが見つかりません");

        course.RemoveComment(commentId, userId);
        await _courseRepository.UpdateAsync(course);
    }

    private static CommentDto ToCommentDto(Comment c)
    {
        UserDto user = c.User is null
            ? new UserDto(c.UserId, string.Empty, string.Empty, UserRole.User.ToString(), DateTime.MinValue)
            : new UserDto(c.User.Id, c.User.Email.Value, c.User.Username.Value, c.User.Role.ToString(), c.User.CreatedAt);
        return new CommentDto(c.Id, c.Body, user, c.CreatedAt, c.UpdatedAt, c.IsEdited);
    }
}
