using Blazored.LocalStorage;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ItemService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorageService;
    private readonly SessionStore _sessionStore;
    private readonly LocalAppStateStore _localAppStateStore;

    public ItemService(
        IHttpClientFactory factory,
        ILocalStorageService localStorage,
        SessionStore sessionStore,
        LocalAppStateStore localAppStateStore)
    {
        _httpClient = factory.CreateClient("AppApi");
        _localStorageService = localStorage;
        _sessionStore = sessionStore;
        _localAppStateStore = localAppStateStore;
    }

    public async Task<BaseItem?> GetItemAsync()
    {
        if (await IsGuestModeAsync())
        {
            var state = await _localAppStateStore.GetStateAsync();
            return state.Tasks.Cast<BaseItem>().Concat(state.Reminders).FirstOrDefault();
        }

        return await _httpClient.GetFromJsonAsync<BaseItem>("Prod/Calendar/item");
    }

    public async Task<BaseItem?> CreateItemAsync(BaseItem item)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync<BaseItem?>(state =>
            {
                switch (item)
                {
                    case TaskItem task:
                        {
                            var createdTask = new TaskItem
                            {
                                // Id = string.IsNullOrWhiteSpace(task.Id) ? Guid.NewGuid().ToString("N") : task.Id,
                                // UserId = SessionStore.GuestUserId,
                                parentTaskId = task.parentTaskId,
                                Title = task.Title.Trim(),
                                Description = task.Description,
                                Progress = Math.Clamp(task.Progress, 0, 100),
                                IsCompleted = task.IsCompleted,
                                CompletionDate = task.IsCompleted ? task.CompletionDate ?? DateTime.UtcNow : null,
                                DueDate = task.DueDate,
                                EntityType = "TASK",
                                // CreatedAt = DateTime.UtcNow,
                                // UpdatedAt = DateTime.UtcNow
                            };

                            state.Tasks.Add(createdTask);
                            return createdTask;
                        }
                    case Reminder reminder:
                        {
                            var createdReminder = new Reminder
                            {
                                // Id = string.IsNullOrWhiteSpace(reminder.Id) ? Guid.NewGuid().ToString("N") : reminder.Id,
                                // UserId = SessionStore.GuestUserId,
                                Name = reminder.Name.Trim(),
                                Description = reminder.Description,
                                IsAcknowledged = reminder.IsAcknowledged,
                                ReminderDate = reminder.ReminderDate,
                                EntityType = "REMINDER",
                                // CreatedAt = DateTime.UtcNow,
                                // UpdatedAt = DateTime.UtcNow
                            };

                            state.Reminders.Add(createdReminder);
                            return createdReminder;
                        }
                    default:
                        return null;
                }
            });
        }

        // options and adding camel casing
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        object payload = item switch
        {
            TaskItem t => new
            {

                EntityType = t.EntityType,
                ParentTaskId = t.parentTaskId,
                Title = t.Title?.Trim(),
                Description = t.Description,
                Progress = Math.Clamp(t.Progress, 0, 100),
                IsCompleted = t.IsCompleted,
                CompletionDate = t.IsCompleted ? t.CompletionDate : null,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            },
            Reminder r => new
            {
                EntityType = r.EntityType,
                Name = r.Name?.Trim(),
                Description = r.Description,
                IsAcknowledged = r.IsAcknowledged,
                ReminderDate = r.ReminderDate,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            },
            _ => item
        };

        // actualy response

        var response = await _httpClient.PostAsJsonAsync("Prod/calendar/item", payload, options);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<BaseItem>() : null;
    }

    public async Task<string> DeleteItemAsync(string itemId)
    {
        if (await IsGuestModeAsync())
        {
            await _localAppStateStore.UpdateAsync(state =>
            {
                state.Tasks.RemoveAll(task => task.Id == itemId);
                state.Reminders.RemoveAll(reminder => reminder.Id == itemId);
            });

            return "Item deleted";
        }

        var response = await _httpClient.DeleteFromJsonAsync<MessageResponse>($"Prod/calendar/item/{itemId}");
        return response != null ? response.message : "Error deleting item";
    }

    public async Task<BaseItem?> UpdateItemAsync(BaseItem item)
    {
        // For guest mode, we need to update the item in the local state store instead of making an API call
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync<BaseItem?>(state =>
            {
                switch (item)
                {
                    case TaskItem task:
                        {
                            var existingTask = state.Tasks.FirstOrDefault(existing => existing.Id == task.Id);
                            if (existingTask is null)
                            {
                                return null;
                            }

                            existingTask.Title = task.Title.Trim();
                            existingTask.Description = task.Description;
                            existingTask.Progress = Math.Clamp(task.Progress, 0, 100);
                            existingTask.IsCompleted = task.IsCompleted;
                            existingTask.CompletionDate = task.IsCompleted ? task.CompletionDate ?? DateTime.UtcNow : null;
                            existingTask.DueDate = task.DueDate;
                            existingTask.UpdatedAt = DateTime.UtcNow;

                            return existingTask;
                        }
                    case Reminder reminder:
                        {
                            var existingReminder = state.Reminders.FirstOrDefault(existing => existing.Id == reminder.Id);
                            if (existingReminder is null)
                            {
                                return null;
                            }

                            existingReminder.Name = reminder.Name.Trim();
                            existingReminder.Description = reminder.Description;
                            existingReminder.IsAcknowledged = reminder.IsAcknowledged;
                            existingReminder.ReminderDate = reminder.ReminderDate;
                            existingReminder.UpdatedAt = DateTime.UtcNow;

                            return existingReminder;
                        }
                    default:
                        return null;
                }
            });
        }

        // For API mode, we proceed with the PUT request as before, but we ensure that the payload is correctly formatted

        Console.WriteLine($"Updating item with ID: '{item.Id}' and type: '{item.EntityType}'");
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // We create a payload object that only includes the properties relevant to the item type, and we ensure that string properties are trimmed and progress is clamped
        object payload = item switch
        {
            TaskItem t => new
            {
                Id = t.Id,
                EntityType = t.EntityType,
                ParentTaskId = t.parentTaskId,
                Title = t.Title?.Trim(),
                Description = t.Description,
                Progress = Math.Clamp(t.Progress, 0, 100),
                IsCompleted = t.IsCompleted,
                CompletionDate = t.IsCompleted ? t.CompletionDate : null,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            },
            Reminder r => new
            {   
                Id = r.Id,
                EntityType = r.EntityType,
                Name = r.Name?.Trim(),
                Description = r.Description,
                IsAcknowledged = r.IsAcknowledged,
                ReminderDate = r.ReminderDate,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            },
            _ => item
        };

        Console.WriteLine($"PUT ID: '{item.Id}'");

        var response = await _httpClient.PutAsJsonAsync($"Prod/calendar/item/{item.Id}", payload, options);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<BaseItem>() : null;
    }

    private async Task<bool> IsGuestModeAsync()
    {
        return await _sessionStore.GetGuestSessionAsync() is not null;
    }
}
