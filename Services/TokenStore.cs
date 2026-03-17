using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.JSInterop;

public class TokenStore
{
    private const string KEY = "auth_tokens"; // name of the key in local storage

    private readonly ILocalStorageService _storage;

    public TokenStore(ILocalStorageService storage)
    {
        _storage = storage;
    }

    public async Task SaveTokensAsync(AuthTokens tokens)
    {
        await _storage.SetItemAsync(KEY, tokens);
    }

    public async Task<AuthTokens?> GetTokensAsync()
    {
        return await _storage.GetItemAsync<AuthTokens>(KEY);
    }

    public async Task RemoveTokensAsync()
    {
        await _storage.RemoveItemAsync(KEY);
        
    }


}

