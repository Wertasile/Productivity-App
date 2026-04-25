using System.Net.Http.Json;

public class TaskBoardService
{
    private readonly HttpClient _httpClient;
    private readonly SessionStore _sessionStore;
    private readonly LocalAppStateStore _localAppStateStore;

    public TaskBoardService(
        IHttpClientFactory factory,
        SessionStore sessionStore,
        LocalAppStateStore localAppStateStore)
    {
        _httpClient = factory.CreateClient("AppApi");
        _sessionStore = sessionStore;
        _localAppStateStore = localAppStateStore;
    }

    public async Task<TaskBoardResponse> GetTaskBoardAsync()
    {
        if (await IsGuestModeAsync())
        {
            var state = await _localAppStateStore.GetStateAsync();
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek).Date;
            var weekEnd = weekStart.AddDays(6).Date;

            var response = new TaskBoardResponse
            {
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                Items = Enumerable.Range(0, 7)
                    .Select(offset => weekStart.AddDays(offset))
                    .ToDictionary(day => day.ToString("yyyy-MM-dd"), _ => new DateItems())
            };

            foreach (var task in state.Tasks.Where(task => task.DueDate.Date >= weekStart && task.DueDate.Date <= weekEnd))
            {
                response.Items[task.DueDate.ToString("yyyy-MM-dd")].Tasks.Add(task);
            }

            foreach (var reminder in state.Reminders.Where(reminder => reminder.ReminderDate.Date >= weekStart && reminder.ReminderDate.Date <= weekEnd))
            {
                response.Items[reminder.ReminderDate.ToString("yyyy-MM-dd")].Reminders.Add(reminder);
            }

            foreach (var note in state.Notes.Where(note => note.UpdatedAt.Date >= weekStart && note.UpdatedAt.Date <= weekEnd))
            {
                response.Items[note.UpdatedAt.ToString("yyyy-MM-dd")].Notes.Add(note);
            }

            return response;
        }

        return await _httpClient.GetFromJsonAsync<TaskBoardResponse>("/Prod/taskboard/week") ?? new TaskBoardResponse
        {
            Items = new Dictionary<string, DateItems>()
        };
    }

    private async Task<bool> IsGuestModeAsync()
    {
        return await _sessionStore.GetGuestSessionAsync() is not null;
    }
}
