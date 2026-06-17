using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Courses.Queries.GetComments;
using RunRoutes.Core.Users.Dtos;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.Queries.Courses;

public class GetCommentsByCourseIdQueryHandler(AppDbContext db)
    : IQueryHandler<GetCommentsByCourseIdQuery, GetCommentsResponse>
{
    private readonly AppDbContext _db = db;

    public async Task<GetCommentsResponse> HandleAsync(
        GetCommentsByCourseIdQuery query,
        CancellationToken cancellationToken)
    {
        var raw = await _db.Courses
            .AsNoTracking()
            .Where(c => c.Id == query.CourseId)
            .Select(c => new
            {
                Comments = c.Comments
                .OrderBy(cm => cm.CreatedAt)
                .Select(cm => new
                {
                    cm.Id,
                    cm.Body,
                    User = new UserDto(
                        cm.User.Id,
                        cm.User.Email.Value,
                        cm.User.Username.Value,
                        cm.User.Role.ToString(),
                        cm.User.CreatedAt
                    ),
                    cm.CreatedAt,
                    cm.UpdatedAt,
                    cm.IsEdited
                })
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (raw is null)
            throw new NotFoundException("コースが見つかりません");

        var dtos = raw.Comments.Select(cm => new CommentDto(
            cm.Id,
            cm.Body,
            cm.User,
            cm.CreatedAt,
            cm.UpdatedAt,
            cm.IsEdited
        ));

        return new GetCommentsResponse(dtos);
    }
}