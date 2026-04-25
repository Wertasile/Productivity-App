using System.Net.Http.Json;
using Blazored.LocalStorage;

public class FolderService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly SessionStore _sessionStore;
    private readonly LocalAppStateStore _localAppStateStore;
    private readonly string api = "folders";

    public FolderService(
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

    public async Task<List<Folder>> GetFolders()
    {
        if (await IsGuestModeAsync())
        {
            var state = await _localAppStateStore.GetStateAsync();
            return state.Folders.OrderBy(folder => folder.Name).ToList();
        }

        return await _httpClient.GetFromJsonAsync<List<Folder>>($"Prod/folders/") ?? new List<Folder>();
    }

    public async Task<FolderContent> GetFolder(string id)
    {
        if (await IsGuestModeAsync())
        {
            var state = await _localAppStateStore.GetStateAsync();
            return BuildFolderContent(state, id);
        }

        return await _httpClient.GetFromJsonAsync<FolderContent>($"Prod/folders/{id}") ?? new FolderContent();
    }

    public async Task<Folder> CreateFolder(Folder folder)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var parentFolderId = NormalizeParentFolderId(folder.ParentFolderId);
                if (!string.IsNullOrWhiteSpace(parentFolderId) && state.Folders.All(existing => existing.Id != parentFolderId))
                {
                    parentFolderId = null;
                }

                var createdFolder = new Folder
                {
                    Id = string.IsNullOrWhiteSpace(folder.Id) ? Guid.NewGuid().ToString("N") : folder.Id,
                    UserId = SessionStore.GuestUserId,
                    ParentFolderId = parentFolderId,
                    Name = folder.Name.Trim(),
                    Description = folder.Description,
                    IsQuickAccess = folder.IsQuickAccess,
                    EntityType = "FOLDER",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                state.Folders.Add(createdFolder);
                createdFolder.Path = BuildFolderPath(createdFolder, state.Folders);
                return createdFolder;
            });
        }

        var response = await _httpClient.PostAsJsonAsync($"Prod/folders", folder);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<Folder>() ?? new Folder() : new Folder();
    }

    public async Task<string> DeleteFolder(Folder folder)
    {
        if (await IsGuestModeAsync())
        {
            await _localAppStateStore.UpdateAsync(state =>
            {
                var folderIdsToDelete = CollectDescendantFolderIds(folder.Id, state.Folders);
                folderIdsToDelete.Add(folder.Id);

                state.Folders.RemoveAll(existingFolder => folderIdsToDelete.Contains(existingFolder.Id));
                state.Notes.RemoveAll(note => note.FolderId is not null && folderIdsToDelete.Contains(note.FolderId));
            });

            return "Folder deleted";
        }

        var response = await _httpClient.DeleteFromJsonAsync<MessageResponse>($"{api}/{folder.Id}");
        return response != null ? response.message : "Error deleting folder";
    }

    public async Task<Folder> UpdateFolder(Folder folder)
    {
        if (await IsGuestModeAsync())
        {
            return await _localAppStateStore.UpdateAsync(state =>
            {
                var existingFolder = state.Folders.FirstOrDefault(existing => existing.Id == folder.Id);

                if (existingFolder is null)
                {
                    return new Folder();
                }

                var proposedParentFolderId = NormalizeParentFolderId(folder.ParentFolderId);
                var descendantIds = CollectDescendantFolderIds(existingFolder.Id, state.Folders);

                if (proposedParentFolderId == existingFolder.Id || (!string.IsNullOrWhiteSpace(proposedParentFolderId) && descendantIds.Contains(proposedParentFolderId)))
                {
                    proposedParentFolderId = existingFolder.ParentFolderId;
                }

                existingFolder.Name = folder.Name.Trim();
                existingFolder.Description = folder.Description;
                existingFolder.IsQuickAccess = folder.IsQuickAccess;
                existingFolder.ParentFolderId = proposedParentFolderId;
                existingFolder.UpdatedAt = DateTime.UtcNow;
                existingFolder.Path = BuildFolderPath(existingFolder, state.Folders);

                RefreshDescendantPaths(existingFolder.Id, state.Folders);
                return existingFolder;
            });
        }

        var response = await _httpClient.PutAsJsonAsync($"Prod/folders/{folder.Id}", folder);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<Folder>() ?? new Folder() : new Folder();
    }

    private async Task<bool> IsGuestModeAsync()
    {
        return await _sessionStore.GetGuestSessionAsync() is not null;
    }

    private static FolderContent BuildFolderContent(LocalAppState state, string folderId)
    {
        if (string.Equals(folderId, "ROOT", StringComparison.OrdinalIgnoreCase))
        {
            return new FolderContent
            {
                Folders = state.Folders
                    .Where(folder => string.IsNullOrWhiteSpace(folder.ParentFolderId))
                    .OrderBy(folder => folder.Name)
                    .ToList(),
                Notes = state.Notes
                    .Where(note => string.IsNullOrWhiteSpace(note.FolderId))
                    .OrderByDescending(note => note.UpdatedAt)
                    .ToList()
            };
        }

        return new FolderContent
        {
            Folders = state.Folders
                .Where(folder => folder.ParentFolderId == folderId)
                .OrderBy(folder => folder.Name)
                .ToList(),
            Notes = state.Notes
                .Where(note => note.FolderId == folderId)
                .OrderByDescending(note => note.UpdatedAt)
                .ToList()
        };
    }

    private static string? NormalizeParentFolderId(string? parentFolderId)
    {
        return string.IsNullOrWhiteSpace(parentFolderId) || parentFolderId == "ROOT" ? null : parentFolderId;
    }

    private static string BuildFolderPath(Folder folder, IReadOnlyCollection<Folder> allFolders)
    {
        var segments = new Stack<string>();
        Folder? currentFolder = folder;

        while (currentFolder is not null)
        {
            if (!string.IsNullOrWhiteSpace(currentFolder.Name))
            {
                segments.Push(currentFolder.Name.Trim());
            }

            if (string.IsNullOrWhiteSpace(currentFolder.ParentFolderId))
            {
                break;
            }

            currentFolder = allFolders.FirstOrDefault(existing => existing.Id == currentFolder.ParentFolderId);
        }

        return segments.Count == 0 ? "/" : $"/{string.Join("/", segments)}/";
    }

    private static void RefreshDescendantPaths(string folderId, List<Folder> folders)
    {
        foreach (var childFolder in folders.Where(existing => existing.ParentFolderId == folderId).ToList())
        {
            childFolder.Path = BuildFolderPath(childFolder, folders);
            RefreshDescendantPaths(childFolder.Id, folders);
        }
    }

    private static HashSet<string> CollectDescendantFolderIds(string folderId, IEnumerable<Folder> folders)
    {
        var foldersByParent = folders
            .GroupBy(folder => folder.ParentFolderId ?? string.Empty)
            .ToDictionary(group => group.Key, group => group.ToList());

        var result = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(folderId);

        while (queue.Count > 0)
        {
            var currentFolderId = queue.Dequeue();

            if (!foldersByParent.TryGetValue(currentFolderId, out var childFolders))
            {
                continue;
            }

            foreach (var childFolder in childFolders)
            {
                if (result.Add(childFolder.Id))
                {
                    queue.Enqueue(childFolder.Id);
                }
            }
        }

        return result;
    }
}
