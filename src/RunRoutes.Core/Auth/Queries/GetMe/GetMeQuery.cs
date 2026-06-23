using RunRoutes.Core.Common.Queries;
using RunRoutes.Core.Users.Dtos;

namespace RunRoutes.Core.Auth.Queries.GetMe;

public record GetMeQuery(Guid UserId) : IQuery<MeResponse>;