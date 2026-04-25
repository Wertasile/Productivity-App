using Blazored.LocalStorage;

public class SessionStore
{
    public const string GuestUserId = "guest-local-user";
    private const string GuestSessionKey = "guest_session";

    private readonly ILocalStorageService _storage;

    public SessionStore(ILocalStorageService storage)
    {
        _storage = storage;
    }

    public async Task SaveGuestSessionAsync(GuestSession session)
    {
        await _storage.SetItemAsync(GuestSessionKey, session);
    }

    public async Task<GuestSession?> GetGuestSessionAsync()
    {
        return await _storage.GetItemAsync<GuestSession>(GuestSessionKey);
    }

    public async Task RemoveGuestSessionAsync()
    {
        await _storage.RemoveItemAsync(GuestSessionKey);
    }
}
