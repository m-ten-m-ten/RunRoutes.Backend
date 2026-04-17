using RunRoutes.Core.DTOs.Common;

namespace RunRoutes.Core.DTOs.Comments;

public record CommentDto(Guid Id, string Body, UserDto User, DateTime CreatedAt, DateTime UpdatedAt, bool IsEdited);
public record GetCommentsResponse(IEnumerable<CommentDto> Comments);
public record CreateCommentRequest(string Body);
public record CreateCommentResponse(CommentDto Comment);
public record UpdateCommentRequest(string Body);
public record UpdateCommentResponse(CommentDto Comment);
