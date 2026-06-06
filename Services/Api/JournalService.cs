using Blazored.LocalStorage;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public class JournalService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly SessionStore _sessionStore;
    private readonly LocalAppStateStore _localAppStateStore;

    public JournalService(
        IHttpClientFactory factory,
        ILocalStorageService localStorage,
        SessionStore sessionStore,
        LocalAppStateStore localAppStateStore)
    {
        _httpClient = factory.CreateClient("AppApi");
        _localStorage = localStorage;
        _sessionStore = sessionStore;
        _localAppStateStore = localAppStateStore;
    }

    public async Task<JournalItem?> GetJournalByDateAsync(string date)
    {
        if (await IsGuestModeAsync())
        {
            var state = await _localAppStateStore.GetStateAsync();
            return state.Journals.FirstOrDefault(j => j.Date.ToString("yyyy-MM-dd") == date);
        }

        var response = await _httpClient.GetAsync($"Prod/journal/item?date={date}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // No journal exists for the requested date
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get journal: {response.StatusCode} - {body}");
        }

        return await response.Content.ReadFromJsonAsync<JournalItem>();
    }

    public async Task<JournalItem?> CreateJournal(JournalItem journal)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var created = new JournalItem
                {
                    Id = string.IsNullOrWhiteSpace(journal.Id) ? Guid.NewGuid().ToString("N") : journal.Id,
                    UserId = SessionStore.GuestUserId,
                    Content = journal.Content,
                    Date = journal.Date,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                state.Journals.Add(created);
                return created;
            });
        }

        // options and adding camel casing
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var response = await _httpClient.PostAsJsonAsync("Prod/journal/item", journal,options);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to create journal: {response.StatusCode}");

        return await response.Content.ReadFromJsonAsync<JournalItem>();
    }

    public async Task<JournalItem> UpdateJournal(JournalItem journal)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var existing = state.Journals.FirstOrDefault(j => j.Id == journal.Id);
                if (existing is null) return new JournalItem();

                existing.Content = journal.Content;
                existing.UpdatedAt = DateTime.UtcNow;
                return existing;
            });
        }

        var dateParam = journal.Date.ToString("yyyy-MM-dd");
        // options and adding camel casing
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var response = await _httpClient.PutAsJsonAsync($"Prod/journal/item?date={dateParam}", journal, options);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to update journal: {response.StatusCode}");

        return await response.Content.ReadFromJsonAsync<JournalItem>() ?? new JournalItem();
    }

    public async Task DeleteJournal(string date)
    {
        if (await IsGuestModeAsync())
        {
            await _localAppStateStore.UpdateAsync(state =>
            {
                state.Journals.RemoveAll(j => j.Date.ToString("yyyy-MM-dd") == date);
            });
            return;
        }

        await _httpClient.DeleteAsync($"Prod/journal/item?date={date}");
    }

    private async Task<bool> IsGuestModeAsync()
    {
        return await _sessionStore.GetGuestSessionAsync() is not null;
    }
}