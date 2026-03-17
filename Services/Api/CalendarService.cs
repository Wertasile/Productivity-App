
using System.Net.Http.Json;

public class CalendarService
{
    private readonly HttpClient _httpClient;
    
    public CalendarService(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("AppApi");
    }
    
    public async Task<CalendarResponse> GetMonthAsync()
    {
        return await _httpClient.GetFromJsonAsync<CalendarResponse>("api/calendar/month");
    }

    public async Task<DayResponse> GetDayAsync()
    {
        return await _httpClient.GetFromJsonAsync<DayResponse>("api/calendar/day");
    }



}

