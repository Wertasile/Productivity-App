public class AppSessionService
{
    private readonly SessionStore _sessionStore;
    private readonly TokenStore _tokenStore;
    private readonly CustomAuthStateProvider _authStateProvider;

    public AppSessionService(
        SessionStore sessionStore,
        TokenStore tokenStore,
        CustomAuthStateProvider authStateProvider)
    {
        _sessionStore = sessionStore;
        _tokenStore = tokenStore;
        _authStateProvider = authStateProvider;
    }

    public async Task ContinueAsGuestAsync(string? displayName = null)
    {
        await _tokenStore.RemoveTokensAsync();

        await _sessionStore.SaveGuestSessionAsync(new GuestSession
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Guest" : displayName.Trim(),
            StartedAtUtc = DateTime.UtcNow
        });

        _authStateProvider.NotifyUserAuthentication();
    }

    public Task<GuestSession?> GetGuestSessionAsync()
    {
        return _sessionStore.GetGuestSessionAsync();
    }

    public async Task<bool> IsGuestModeAsync()
    {
        return await _sessionStore.GetGuestSessionAsync() is not null;
    }
}
