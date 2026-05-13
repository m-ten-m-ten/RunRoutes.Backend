using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Courses.Dtos;

public record CommentDto(Guid Id, string Body, UserDto User, DateTime CreatedAt, DateTime? UpdatedAt, bool IsEdited);
public record GetCommentsResponse(IEnumerable<CommentDto> Comments);
public record CreateCommentRequest(string Body);
public record CreateCommentResponse(CommentDto Comment);
public record UpdateCommentRequest(string Body);
public record UpdateCommentResponse(CommentDto Comment);
