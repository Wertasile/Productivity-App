using Blazored.LocalStorage;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ProjectService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly SessionStore _sessionStore;
    private readonly LocalAppStateStore _localAppStateStore;

    public ProjectService(
        IHttpClientFactory factory,
        ILocalStorageService localStorage,
        SessionStore sessionStore,
        LocalAppStateStore localAppStateStore)
    {
        _httpClient = factory.CreateClient("AppApi");
        _localStorage = localStorage;
        _sessionStore = sessionStore;
        _localAppStateStore = localAppStateStore;
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        if (await IsGuestModeAsync())
        {
            var state = await _localAppStateStore.GetStateAsync();
            return state.Projects.OrderByDescending(project => project.UpdatedAt).ToList();
        }

        return await _httpClient.GetFromJsonAsync<List<Project>>("Prod/projects") ?? new List<Project>();
    }

    public async Task<Project> GetProjectAsync(string id)
    {
        if (await IsGuestModeAsync())
        {
            var state = await _localAppStateStore.GetStateAsync();
            return state.Projects.FirstOrDefault(project => project.Id == id) ?? new Project();
        }

        return await _httpClient.GetFromJsonAsync<Project>($"Prod/projects/{id}") ?? new Project();
    }

    public async Task<Project?> CreateProject(Project project)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var createdProject = new Project
                {
                    Id = string.IsNullOrWhiteSpace(project.Id) ? Guid.NewGuid().ToString("N") : project.Id,
                    UserId = SessionStore.GuestUserId,
                    Name = project.Name.Trim(),
                    Description = project.Description,
                    Start = project.Start,
                    End = project.End,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                state.Projects.Add(createdProject);
                return createdProject;
            });
        }

        // Ensure that the JSON payload uses PascalCase to match the API's expected format
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        object payload = new
        {
            Name = project.Name.Trim(),
            Description = project.Description,
            Start = project.Start,
            End = project.End,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // actualy response

        var response = await _httpClient.PostAsJsonAsync("Prod/projects", payload, options);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<Project>();
    }

    public async Task<string> DeleteProject(string projectId)
    {
        if (await IsGuestModeAsync())
        {
            await _localAppStateStore.UpdateAsync(state =>
            {
                state.Projects.RemoveAll(existingProject => existingProject.Id == projectId);
            });

            return "Project deleted";
        }

        var response = await _httpClient.DeleteFromJsonAsync<MessageResponse>($"Prod/projects/{projectId}");
        return response != null ? response.message : "Error deleting project";
    }

    public async Task<Project> UpdateProject(Project project)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var existingProject = state.Projects.FirstOrDefault(existing => existing.Id == project.Id);

                if (existingProject is null)
                {
                    return new Project();
                }

                existingProject.Name = project.Name.Trim();
                existingProject.Description = project.Description;
                existingProject.Start = project.Start;
                existingProject.End = project.End;
                existingProject.UpdatedAt = DateTime.UtcNow;

                return existingProject;
            });
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var response = await _httpClient.PutAsJsonAsync($"Prod/projects/{project.Id}", project, options);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to update Project : {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<Project>() ?? new Project();
    }

    // ------------------------------------------ PROJECT NOTE API CALLS -------------------------------------------

    public async Task<Note?> CreateProjectNote(Note note)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var createdNote = new Note
                {
                    Id = string.IsNullOrWhiteSpace(note.Id) ? Guid.NewGuid().ToString("N") : note.Id,
                    UserId = SessionStore.GuestUserId,
                    FolderId = string.IsNullOrWhiteSpace(note.FolderId) ? null : note.FolderId,
                    Title = note.Title.Trim(),
                    Content = note.Content,
                    EntityType = "NOTE",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                state.Notes.Add(createdNote);
                return createdNote;
            });
        }

        var response = await _httpClient.PostAsJsonAsync("Prod/note/", note);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create Note : {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<Note>();
    }

    public async Task<string> DeleteProjectNote(string noteId)
    {
        if (await IsGuestModeAsync())
        {
            await _localAppStateStore.UpdateAsync(state =>
            {
                state.Notes.RemoveAll(existingNote => existingNote.Id == noteId);
            });

            return "Note deleted";
        }

        var response = await _httpClient.DeleteFromJsonAsync<MessageResponse>($"notes/{noteId}");
        return response != null ? response.message : "Error deleting note";
    }

    public async Task<Note> UpdateProjectNote(Note note)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var existingNote = state.Notes.FirstOrDefault(existing => existing.Id == note.Id);

                if (existingNote is null)
                {
                    return new Note();
                }

                existingNote.Title = note.Title.Trim();
                existingNote.Content = note.Content;
                existingNote.FolderId = string.IsNullOrWhiteSpace(note.FolderId) ? null : note.FolderId;
                existingNote.UpdatedAt = DateTime.UtcNow;

                return existingNote;
            });
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var response = await _httpClient.PutAsJsonAsync("notes/", note, options);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to update Note : {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<Note>() ?? new Note();
    }

    // ------------------------------------------ PHASE API CALLS ------------------------------------------------
    public async Task<Phase> CreatePhase(Phase phase, string projectId)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var project = state.Projects.FirstOrDefault(p => p.Id == projectId);
                if (project is null) return new Phase();

                var createdPhase = new Phase
                {
                    Id = string.IsNullOrWhiteSpace(phase.Id) ? Guid.NewGuid().ToString("N") : phase.Id,
                    Name = phase.Name.Trim(),
                    Description = phase.Description,
                    Start = phase.Start,
                    End = phase.End,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                project.Phases.Add(createdPhase);
                return createdPhase;
            });
        }

        // Ensure that the JSON payload uses PascalCase to match the API's expected format
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        object payload = new
        {
            Name = phase.Name.Trim(),
            Description = phase.Description,
            Start = phase.Start,
            End = phase.End,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // actually response

        var response = await _httpClient.PostAsJsonAsync($"Prod/projects/{projectId}/phases", payload, options);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create Phase : {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<Phase>() ?? new Phase();

    }

    public async Task<Phase> UpdatePhase(Phase phase)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var project = state.Projects.FirstOrDefault(p => p.Phases.Any(ph => ph.Id == phase.Id));
                if (project is null) return new Phase();

                var existingPhase = project.Phases.FirstOrDefault(existing => existing.Id == phase.Id);
                if (existingPhase is null) return new Phase();

                existingPhase.Name = phase.Name.Trim();
                existingPhase.Description = phase.Description;
                existingPhase.Start = phase.Start;
                existingPhase.End = phase.End;
                existingPhase.UpdatedAt = DateTime.UtcNow;

                return existingPhase;
            });
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var response = await _httpClient.PutAsJsonAsync($"Prod/phases/{phase.Id}", phase, options);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to update Phase : {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<Phase>() ?? new Phase();
    }

    public async Task<string> DeletePhase(string phaseId)
    {
        if (await IsGuestModeAsync())
        {
            await _localAppStateStore.UpdateAsync(state =>
            {
                var project = state.Projects.FirstOrDefault(p => p.Phases.Any(ph => ph.Id == phaseId));
                if (project is null) return;

                project.Phases.RemoveAll(existingPhase => existingPhase.Id == phaseId);
            });

            return "Phase deleted";
        }

        var response = await _httpClient.DeleteFromJsonAsync<MessageResponse>($"phases/{phaseId}");
        return response != null ? response.message : "Error deleting phase";
    }

    // ------------------------------------------- PHASE TASK API CALLS ------------------------------------------

    public async Task<TaskItem> CreatePhaseTask(TaskItem task, string phaseId)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var project = state.Projects.FirstOrDefault(p => p.Phases.Any(ph => ph.Id == phaseId));
                if (project is null) return new TaskItem();

                var phase = project.Phases.FirstOrDefault(ph => ph.Id == phaseId);
                if (phase is null) return new TaskItem();

                var createdTask = new TaskItem
                {
                    Id = string.IsNullOrWhiteSpace(task.Id) ? Guid.NewGuid().ToString("N") : task.Id,
                    Title = task.Title.Trim(),
                    Description = task.Description,
                    Priority = Math.Clamp(task.Priority, 0, 3),
                    Status = task.Status,
                    CompletionDate = task.CompletionDate,
                    Start = task.Start,
                    End = task.End,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                phase.Tasks.Add(createdTask);
                return createdTask;
            });
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var response = await _httpClient.PostAsJsonAsync($"Prod/phases/{phaseId}/tasks", task, options);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create Task : {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<TaskItem>();
    }

    public async Task<TaskItem> UpdatePhaseTask(TaskItem task)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var project = state.Projects.FirstOrDefault(p => p.Phases.Any(ph => ph.Tasks.Any(t => t.Id == task.Id)));
                if (project is null) return new TaskItem();

                var phase = project.Phases.FirstOrDefault(ph => ph.Tasks.Any(t => t.Id == task.Id));
                if (phase is null) return new TaskItem();

                var existingTask = phase.Tasks.FirstOrDefault(existing => existing.Id == task.Id);
                if (existingTask is null) return new TaskItem();

                existingTask.Title = task.Title.Trim();
                existingTask.Description = task.Description;
                existingTask.Priority = Math.Clamp(task.Priority, 0, 3);
                existingTask.Status = task.Status;
                existingTask.CompletionDate = task.CompletionDate;
                existingTask.Start = task.Start;
                existingTask.End = task.End;
                existingTask.UpdatedAt = DateTime.UtcNow;

                return existingTask;
            });
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var response = await _httpClient.PutAsJsonAsync($"Prod/tasks/{task.Id}", task, options);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to update Task : {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<TaskItem>() ?? new TaskItem();
    }

    public async Task<string> DeletePhaseTask(string taskId)
    {
        if (await IsGuestModeAsync())
        {
            await _localAppStateStore.UpdateAsync(state =>
            {
                var project = state.Projects.FirstOrDefault(p => p.Phases.Any(ph => ph.Tasks.Any(t => t.Id == taskId)));
                if (project is null) return;

                var phase = project.Phases.FirstOrDefault(ph => ph.Tasks.Any(t => t.Id == taskId));
                if (phase is null) return;

                phase.Tasks.RemoveAll(existingTask => existingTask.Id == taskId);
            });

            return "Task deleted";
        }

        var response = await _httpClient.DeleteFromJsonAsync<MessageResponse>($"tasks/{taskId}");
        return response != null ? response.message : "Error deleting task";
    }

    // ------------------------------------------ PHASE NOTE API CALLS -------------------------------------------

    public async Task<Note?> CreatePhaseNote(Note note)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var createdNote = new Note
                {
                    Id = string.IsNullOrWhiteSpace(note.Id) ? Guid.NewGuid().ToString("N") : note.Id,
                    UserId = SessionStore.GuestUserId,
                    FolderId = string.IsNullOrWhiteSpace(note.FolderId) ? null : note.FolderId,
                    Title = note.Title.Trim(),
                    Content = note.Content,
                    EntityType = "NOTE",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                state.Notes.Add(createdNote);
                return createdNote;
            });
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var response = await _httpClient.PostAsJsonAsync("Prod/note/", note, options);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to create Note : {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<Note>();
    }

    public async Task<string> DeletePhaseNote(string noteId)
    {
        if (await IsGuestModeAsync())
        {
            await _localAppStateStore.UpdateAsync(state =>
            {
                state.Notes.RemoveAll(existingNote => existingNote.Id == noteId);
            });

            return "Note deleted";
        }

        var response = await _httpClient.DeleteFromJsonAsync<MessageResponse>($"notes/{noteId}");
        return response != null ? response.message : "Error deleting note";
    }

    public async Task<Note> UpdatePhaseNote(Note note)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var existingNote = state.Notes.FirstOrDefault(existing => existing.Id == note.Id);

                if (existingNote is null)
                {
                    return new Note();
                }

                existingNote.Title = note.Title.Trim();
                existingNote.Content = note.Content;
                existingNote.FolderId = string.IsNullOrWhiteSpace(note.FolderId) ? null : note.FolderId;
                existingNote.UpdatedAt = DateTime.UtcNow;

                return existingNote;
            });
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var response = await _httpClient.PutAsJsonAsync("notes/", note, options);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to update Note : {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<Note>() ?? new Note();
    }

    private async Task<bool> IsGuestModeAsync()
    {
        return await _sessionStore.GetGuestSessionAsync() is not null;
    }
}
