using RunRoutes.Core.Courses.Dtos;

namespace RunRoutes.Core.Courses;

public interface ICommentService
{
    Task<GetCommentsResponse> GetByCourseIdAsync(Guid courseId);
    Task<CreateCommentResponse> CreateAsync(Guid courseId, CreateCommentRequest request, Guid userId);
    Task<UpdateCommentResponse> UpdateAsync(Guid courseId, Guid commentId, UpdateCommentRequest request, Guid userId);
    Task DeleteAsync(Guid courseId, Guid commentId, Guid userId);
}
