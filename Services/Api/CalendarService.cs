using System.Net.Http.Json;

public class CalendarService
{
    private readonly HttpClient _httpClient;
    private readonly SessionStore _sessionStore;
    private readonly LocalAppStateStore _localAppStateStore;

    public CalendarService(
        IHttpClientFactory factory,
        SessionStore sessionStore,
        LocalAppStateStore localAppStateStore)
    {
        _httpClient = factory.CreateClient("AppApi");
        _sessionStore = sessionStore;
        _localAppStateStore = localAppStateStore;
    }

    public async Task<CalendarResponse> GetMonthAsync(DateTime? month = null)
    {
        if (await IsGuestModeAsync())
        {
            var currentMonth = month ?? DateTime.Today;
            var firstDayOfMonth = new DateTime(currentMonth.Year, currentMonth.Month, 1);
            var diff = (7 + (firstDayOfMonth.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startDate = firstDayOfMonth.AddDays(-diff).Date;
            var endDate = startDate.AddDays(41).Date;
            var state = await _localAppStateStore.GetStateAsync();
            var response = new CalendarResponse();

            foreach (var note in state.Notes)
            {
                AddToCalendar(response, note.UpdatedAt, item => item.Notes.Add(note), startDate, endDate);
            }

            foreach (var reminder in state.Reminders)
            {
                AddToCalendar(response, reminder.ReminderDate, item => item.Reminders.Add(reminder), startDate, endDate);
            }

            foreach (var task in state.Tasks)
            {
                AddToCalendar(response, task.DueDate, item => item.Tasks.Add(task), startDate, endDate);
            }

            return response;
        }

        return await _httpClient.GetFromJsonAsync<CalendarResponse>("Prod/calendar/month") ?? new CalendarResponse();
    }

    public async Task<DayResponse> GetDayAsync(DateTime? day = null)
    {
        if (await IsGuestModeAsync())
        {
            var selectedDay = (day ?? DateTime.Today).Date;
            var state = await _localAppStateStore.GetStateAsync();

            return new DayResponse
            {
                Notes = state.Notes.Where(note => note.UpdatedAt.Date == selectedDay).ToList(),
                Reminders = state.Reminders.Where(reminder => reminder.ReminderDate.Date == selectedDay).ToList(),
                Tasks = new List<Task>()
            };
        }

        return await _httpClient.GetFromJsonAsync<DayResponse>("Prod/calendar/day") ?? new DayResponse
        {
            Notes = new List<Note>(),
            Reminders = new List<Reminder>(),
            Tasks = new List<Task>()
        };
    }

    private async Task<bool> IsGuestModeAsync()
    {
        return await _sessionStore.GetGuestSessionAsync() is not null;
    }

    private static void AddToCalendar(
        CalendarResponse response,
        DateTime date,
        Action<CalendarItem> addAction,
        DateTime startDate,
        DateTime endDate)
    {
        var day = date.Date;

        if (day < startDate || day > endDate)
        {
            return;
        }

        var key = day.ToString("yyyy-MM-dd");

        if (!response.TryGetValue(key, out var calendarItem))
        {
            calendarItem = new CalendarItem();
            response[key] = calendarItem;
        }

        addAction(calendarItem);
    }
}
