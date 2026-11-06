using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TaskBoard.Api.Http;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetSubjectId(this ClaimsPrincipal principal, out Guid userId)
    {
        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(subject, out userId);
    }
}
