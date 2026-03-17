
using System.Net.Http.Json;
using Blazored.LocalStorage;

public class FolderService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private string api = "folders";

    public FolderService(IHttpClientFactory factory, ILocalStorageService localStorage) { 
        _httpClient = factory.CreateClient("AppApi");
        _localStorage = localStorage;
    }

    // get all folders
    public async Task<List<Folder>> GetFolders()
    {
        return await _httpClient.GetFromJsonAsync<List<Folder>>($"{api}/");
    }

    // get a folder by id or ROOT
    public async Task<FolderContent> GetFolder(string id)
    {
        return await _httpClient.GetFromJsonAsync<FolderContent>($"{api}/{id}");
    }

    public async Task<Folder> CreateFolder(Folder folder)
    {
        var response = await _httpClient.PostAsJsonAsync($"{api}/create", folder);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<Folder>() : null;

    }

    public async Task<string> DeleteFolder(Folder folderId)
    {
        var response = await _httpClient.DeleteFromJsonAsync<MessageResponse>($"{api}/{folderId}");
        return response != null ? response.message : "Error deleting folder";

    }

    public async Task<Folder> UpdateFolder(Folder folder)
    {
        var response = await _httpClient.PutAsJsonAsync($"{api}/create", folder);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<Folder>() : null;
    }

        
}

