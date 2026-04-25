using Blazored.LocalStorage;
using System.Net.Http.Json;

public class NoteService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly SessionStore _sessionStore;
    private readonly LocalAppStateStore _localAppStateStore;

    public NoteService(
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

    public async Task<List<Note>> GetNotesAsync()
    {
        if (await IsGuestModeAsync())
        {
            var state = await _localAppStateStore.GetStateAsync();
            return state.Notes.OrderByDescending(note => note.UpdatedAt).ToList();
        }

        return await _httpClient.GetFromJsonAsync<List<Note>>("notes") ?? new List<Note>();
    }

    public async Task<Note> GetNoteAsync(int id)
    {
        if (await IsGuestModeAsync())
        {
            var state = await _localAppStateStore.GetStateAsync();
            return state.Notes.FirstOrDefault(note => note.Id == id.ToString()) ?? new Note();
        }

        return await _httpClient.GetFromJsonAsync<Note>($"notes/{id}") ?? new Note();
    }

    public async Task<Note?> CreateNote(Note note)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var createdNote = new Note
                {
                    Id = string.IsNullOrWhiteSpace(note.Id) ? Guid.NewGuid().ToString("N") : note.Id,
                    UserId = SessionStore.GuestUserId,
                    FolderId = string.IsNullOrWhiteSpace(note.FolderId) ? null : note.FolderId,
                    Title = note.Title.Trim(),
                    Content = note.Content,
                    EntityType = "NOTE",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                state.Notes.Add(createdNote);
                return createdNote;
            });
        }

        var response = await _httpClient.PostAsJsonAsync("note/", note);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create Note : {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<Note>();
    }

    public async Task<string> DeleteNote(Note note)
    {
        if (await IsGuestModeAsync())
        {
            await _localAppStateStore.UpdateAsync(state =>
            {
                state.Notes.RemoveAll(existingNote => existingNote.Id == note.Id);
            });

            return "Note deleted";
        }

        var response = await _httpClient.DeleteFromJsonAsync<MessageResponse>($"notes/{note.Id}");
        return response != null ? response.message : "Error deleting note";
    }

    public async Task<Note> UpdateNote(Note note)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var existingNote = state.Notes.FirstOrDefault(existing => existing.Id == note.Id);

                if (existingNote is null)
                {
                    return new Note();
                }

                existingNote.Title = note.Title.Trim();
                existingNote.Content = note.Content;
                existingNote.FolderId = string.IsNullOrWhiteSpace(note.FolderId) ? null : note.FolderId;
                existingNote.UpdatedAt = DateTime.UtcNow;

                return existingNote;
            });
        }

        var response = await _httpClient.PutAsJsonAsync("notes/", note);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to update Note : {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<Note>() ?? new Note();
    }

    private async Task<bool> IsGuestModeAsync()
    {
        return await _sessionStore.GetGuestSessionAsync() is not null;
    }
}
