
using System.Net.Http.Json;

public class DashboardService
{
    private readonly HttpClient _httpClient;

    public DashboardService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DashboardResponse?> GetDashboardAsync()
    {
        return await _httpClient.GetFromJsonAsync<DashboardResponse>("api/dashboard");
    }
}

