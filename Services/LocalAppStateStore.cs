using Blazored.LocalStorage;

public class LocalAppStateStore
{
    private const string StorageKey = "guest_local_app_state_v1";

    private readonly ILocalStorageService _storage;

    public LocalAppStateStore(ILocalStorageService storage)
    {
        _storage = storage;
    }

    public async Task<LocalAppState> GetStateAsync()
    {
        var state = await _storage.GetItemAsync<LocalAppState>(StorageKey);

        if (state is not null)
        {
            return state;
        }

        state = new LocalAppState();
        await SaveStateAsync(state);
        return state;
    }

    public async Task SaveStateAsync(LocalAppState state)
    {
        state.UpdatedAtUtc = DateTime.UtcNow;
        await _storage.SetItemAsync(StorageKey, state);
    }

    public async Task<TResult> UpdateAsync<TResult>(Func<LocalAppState, TResult> update)
    {
        var state = await GetStateAsync();
        var result = update(state);
        await SaveStateAsync(state);
        return result;
    }

    public async Task UpdateAsync(Action<LocalAppState> update)
    {
        var state = await GetStateAsync();
        update(state);
        await SaveStateAsync(state);
    }
}
