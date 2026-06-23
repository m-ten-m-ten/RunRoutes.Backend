using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Auth.Queries.GetMe;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Users.Dtos;
using RunRoutes.Infrastructure.Data;

namespace RunRoutes.Infrastructure.Queries.Auth;

public class GetMeQueryHandler(AppDbContext db)
    : IQueryHandler<GetMeQuery, MeResponse>
{
    private readonly AppDbContext _db = db;

    public async Task<MeResponse> HandleAsync(
        GetMeQuery query,
        CancellationToken cancellationToken)
    {
        var userDto = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == query.UserId)
            .Select(u => new UserDto(
                        u.Id,
                        u.Email.Value,
                        u.Username.Value,
                        u.Role.ToString(),
                        u.CreatedAt)
            )
            .FirstOrDefaultAsync(cancellationToken);

        if (userDto is null)
            throw new NotFoundException("ユーザーが見つかりません");
        return new MeResponse(userDto);
    }
}