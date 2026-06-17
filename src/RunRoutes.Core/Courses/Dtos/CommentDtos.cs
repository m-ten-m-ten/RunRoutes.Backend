using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Courses.Dtos;

public record CommentDto(Guid Id, string Body, UserDto User, DateTime CreatedAt, DateTime? UpdatedAt, bool IsEdited);
public record GetCommentsResponse(IEnumerable<CommentDto> Comments);
public record CreateCommentRequest(string Body);
public record CreateCommentResponse(CreateCommentDto Comment);
public record CreateCommentDto(Guid Id);
public record UpdateCommentRequest(string Body);
public record UpdateCommentResponse(UpdateCommentDto Comment);
public record UpdateCommentDto(Guid Id);
