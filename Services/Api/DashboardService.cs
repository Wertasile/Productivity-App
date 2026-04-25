using System.Net.Http.Json;

public class DashboardService
{
    private readonly HttpClient _httpClient;
    private readonly SessionStore _sessionStore;
    private readonly LocalAppStateStore _localAppStateStore;

    public DashboardService(
        IHttpClientFactory factory,
        SessionStore sessionStore,
        LocalAppStateStore localAppStateStore)
    {
        _httpClient = factory.CreateClient("AppApi");
        _sessionStore = sessionStore;
        _localAppStateStore = localAppStateStore;
    }

    public async Task<DashboardResponse> GetDashboardAsync()
    {
        if (await IsGuestModeAsync())
        {
            var state = await _localAppStateStore.GetStateAsync();
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek).Date;
            var response = new DashboardResponse
            {
                RecentNotes = state.Notes
                    .OrderByDescending(note => note.UpdatedAt)
                    .Take(5)
                    .ToList(),
                PinnedItems = new PinnedItems
                {
                    Notes = state.Notes.OrderByDescending(note => note.UpdatedAt).Take(3).ToList(),
                    Reminders = state.Reminders.OrderBy(reminder => reminder.ReminderDate).Take(3).ToList(),
                    Tasks = state.Tasks.OrderBy(task => task.DueDate).Take(3).ToList()
                },
                Folders = state.Folders
                    .Where(folder => folder.IsQuickAccess)
                    .OrderBy(folder => folder.Name)
                    .ToList()
            };

            if (response.Folders.Count == 0)
            {
                response.Folders = state.Folders
                    .Where(folder => string.IsNullOrWhiteSpace(folder.ParentFolderId))
                    .OrderBy(folder => folder.Name)
                    .Take(5)
                    .ToList();
            }

            foreach (var offset in Enumerable.Range(0, 7))
            {
                var currentDay = weekStart.AddDays(offset);
                response.WeekItems[currentDay.DayOfWeek.ToString()] = new WeekItem
                {
                    Tasks = state.Tasks.Where(task => task.DueDate.Date == currentDay).OrderBy(task => task.DueDate).ToList(),
                    Reminders = state.Reminders.Where(reminder => reminder.ReminderDate.Date == currentDay).OrderBy(reminder => reminder.ReminderDate).ToList()
                };
            }

            return response;
        }

        return await _httpClient.GetFromJsonAsync<DashboardResponse>("Prod/dashboard") ?? new DashboardResponse();
    }

    private async Task<bool> IsGuestModeAsync()
    {
        return await _sessionStore.GetGuestSessionAsync() is not null;
    }
}
