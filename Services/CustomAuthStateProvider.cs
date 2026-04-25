using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

// AuthStateProvider provides functionality to manage the authentication state of the user in a Blazor application.
// It retrieves the JWT token from the TokenStore, extracts the claims, and creates a ClaimsPrincipal to represent the authenticated user.
// It also provides methods to notify the application when the user is authenticated or logged out, allowing the UI to update accordingly.

// We can use <AuthorizeView> and [Authorize] attributes in our Blazor components to conditionally render content based on the user's authentication state,
// which is managed by this CustomAuthStateProvider.

// <CascadingAuthenticationState> provides user context to add descendent components, allowing them to access the authentication state and react to changes in the user's authentication status.
// Therefore, it is used as a top level component in the App.razor file to wrap entire app.
public class CustomAuthStateProvider : Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider
{
    private readonly TokenStore _tokenStore;
    private readonly SessionStore _sessionStore;

    public CustomAuthStateProvider(TokenStore tokenStore, SessionStore sessionStore)
    {
        _tokenStore = tokenStore;
        _sessionStore = sessionStore;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var guestSession = await _sessionStore.GetGuestSessionAsync();

        if (guestSession is not null)
        {
            var guestIdentity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, guestSession.UserId),
                    new Claim(ClaimTypes.Name, guestSession.DisplayName),
                    new Claim("auth_mode", "guest")
                },
                "guest");

            return new AuthenticationState(new ClaimsPrincipal(guestIdentity));
        }

        var tokens = await _tokenStore.GetTokensAsync();

        if (tokens == null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokens.AccessToken);

        var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);

    }

    public void NotifyUserAuthentication()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void NotifyUserLogout()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymousUser)));
    }


}

