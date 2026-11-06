using TaskBoard.Application.Auth;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Abstractions;

public interface IAccessTokenIssuer
{
    AccessTokenDto Issue(UserAccount user);
}
