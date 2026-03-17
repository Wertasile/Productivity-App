
using System.Net.Http.Json;

public class TaskBoardService
{
    private readonly HttpClient _httpClient;
    public TaskBoardService(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("AppApi");
    }

    public async Task<TaskBoardResponse> GetTaskBoardAsync()
    {
        return await _httpClient.GetFromJsonAsync<TaskBoardResponse>("week");
    }

    //public async Task<BaseItem> CreateTaskBoardItemAsync()
    //{
    //    //return await _httpClient.PostAsJsonAsync<>();
    //}

    //public async Task<BaseItem> UpdateTaskBoardItemAsync()
    //{
    //    //var response await _httpClient.PutAsJsonAsync<>();
    //}
}

