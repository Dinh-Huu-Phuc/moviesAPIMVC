using Microsoft.AspNetCore.Identity;

namespace Movie_API.Repositories
{
    public interface ITokenRepository
    {
        string CreateJWTToken(IdentityUser user, List<string> roles);
    }
}
