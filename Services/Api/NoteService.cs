
using Blazored.LocalStorage;
using System.Net.Http.Json;

public class NoteService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;


    public NoteService(IHttpClientFactory factory, ILocalStorageService localStorage)
    {
        _httpClient = factory.CreateClient("AppApi");
        _localStorage = localStorage;
    }

    public async Task<List<Note>> GetNotesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<Note>>("notes");
    }

    public async Task<Note> GetNoteAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<Note>($"notes/{id}");
    }

    public async Task<Note?> CreateNote(Note note)
    {
        
        var response = await _httpClient.PostAsJsonAsync("note/", note);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to create Note : {response.StatusCode}");
        return await response.Content.ReadFromJsonAsync<Note>();
    }

    public async Task<string> DeleteNote(Note noteId)
    {
        var response = await _httpClient.DeleteFromJsonAsync<MessageResponse>($"notes/{noteId}");
        return response != null ? response.message : "Error deleting folder";

    }

    public async Task<Note> UpdateNote(Note note)
    {
        var response = await _httpClient.PutAsJsonAsync<Note>("notes/", note);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to update Note : { response.StatusCode}");
        return await response.Content.ReadFromJsonAsync<Note>();

    }
}

