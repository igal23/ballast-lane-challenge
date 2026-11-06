using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Auth;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Auth;

public sealed class JwtAccessTokenIssuer(
    IOptions<JwtOptions> options,
    TimeProvider timeProvider) : IAccessTokenIssuer
{
    private readonly JwtOptions _options = options.Value;

    public AccessTokenDto Issue(UserAccount user)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(60);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(
                    JwtRegisteredClaimNames.Iat,
                    EpochTime.GetIntDate(now.UtcDateTime).ToString(),
                    ClaimValueTypes.Integer64)
            ],
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new AccessTokenDto(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}
