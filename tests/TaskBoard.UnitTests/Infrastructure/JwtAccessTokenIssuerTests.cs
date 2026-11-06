using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskBoard.Domain.Users;
using TaskBoard.Infrastructure.Auth;

namespace TaskBoard.UnitTests.Infrastructure;

public sealed class JwtAccessTokenIssuerTests
{
    [Fact]
    public void Issue_CreatesValidSignedTokenWithRequiredClaimsAndLifetime()
    {
        var now = new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
        var clock = new FixedTimeProvider(now);
        var options = new JwtOptions
        {
            Issuer = "taskboard-tests",
            Audience = "taskboard-client",
            SigningKey = "a-test-signing-key-with-at-least-32-bytes"
        };
        var issuer = new JwtAccessTokenIssuer(Options.Create(options), clock);
        var user = new UserAccount(Guid.NewGuid(), "user@example.com", "user@example.com", "hash");

        var result = issuer.Issue(user);

        Assert.Equal(now.AddMinutes(60), result.ExpiresAt);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(
            result.AccessToken,
            new TokenValidationParameters
            {
                ValidIssuer = options.Issuer,
                ValidAudience = options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false,
                NameClaimType = ClaimTypes.NameIdentifier
            },
            out var validatedToken);
        var jwt = Assert.IsType<JwtSecurityToken>(validatedToken);
        Assert.Equal(user.Id.ToString(), principal.FindFirstValue(JwtRegisteredClaimNames.Sub));
        Assert.Equal(now, new DateTimeOffset(jwt.ValidFrom, TimeSpan.Zero));
        Assert.Equal(now.AddMinutes(60), new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero));
        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.SignatureAlgorithm);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
