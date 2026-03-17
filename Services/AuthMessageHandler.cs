
using Microsoft.AspNetCore.Components;

public class AuthMessageHandler : DelegatingHandler
{
    private readonly TokenStore _store;
    private readonly AuthService _authService;
    private readonly NavigationManager _navigationManager;

    public AuthMessageHandler(TokenStore TS, AuthService authService, NavigationManager navigationManager)
    {
        _store = TS;
        _authService = authService;
        _navigationManager = navigationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine("Auth handler running");

        await _authService.RefreshTokensIfNeededAsync();

        var tokens = await _store.GetTokensAsync();

        if (tokens != null)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.IdToken);
        }

        // We call the base SendAsync method to send the HTTP request and get the response. This is where the actual HTTP request is made, and we await the response.
        var response = await base.SendAsync(request, cancellationToken);

        // If the response indicates that the user is unauthorized, we log them out and redirect to the login page.
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigationManager.NavigateTo("/login");
        }

        return response;
    }


}

