// LOCAL AUTH SHIM
//
// Provides a ClaimsPrincipal for the local user so Blazor's <AuthorizeView> / [Authorize]
// continue to work without the SAML authentication stack. The principal is always
// "authenticated" and carries the local user's roles.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace _2026WRKJRNL01.UI.Components.Auth
{
    public class LocalAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _state;

        public LocalAuthenticationStateProvider(LocalUserProvider localUser)
        {
            var user = localUser.User;

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.GlobalID),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new(ClaimTypes.Email, user.Email),
            };

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // "LocalAuth" as the auth type marks the identity as authenticated.
            var identity = new ClaimsIdentity(claims, authenticationType: "LocalAuth");
            _state = new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(_state);
    }
}
