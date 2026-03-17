
using Blazored.LocalStorage;
using System.Net.Http.Json;

public class ItemService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorageService;

    public ItemService(IHttpClientFactory factory, ILocalStorageService localStorage)
    {
        _httpClient = factory.CreateClient("AppApi");
        _localStorageService = localStorage;
    }
    
    public async Task<BaseItem?> GetItemAsync()
    {
        return await _httpClient.GetFromJsonAsync<BaseItem>("api/taskboard/item");
    }

    public async Task<BaseItem?> CreateItemAsync(BaseItem item)
    {
        var response = await _httpClient.PostAsJsonAsync("api/taskboard/item", item);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<BaseItem>() : null;
    }

    public async Task<string> DeleteItemAsync(string itemId)
    {
        var response = await _httpClient.DeleteFromJsonAsync<MessageResponse>($"api/taskboard/item/{itemId}");
        return response != null ? response.message : "Error deleting item";
    }

    public async Task<BaseItem?> UpdateItemAsync(BaseItem item)
    {
        var response = await _httpClient.PutAsJsonAsync("api/taskboard/item", item);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<BaseItem>() : null;
    }
}

