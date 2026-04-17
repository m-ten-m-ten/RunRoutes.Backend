using System.Security.Claims;

namespace RunRoutes.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("ユーザーIDが見つかりません");
        return Guid.Parse(sub);
    }
}
