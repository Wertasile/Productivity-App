
public class DashboardResponse
{
    // For Recent Notes
    public List<Note> RecentNotes { get; set; } = new();

    // For Weekly Recap - Includes Note, Reminders and Tasks categorised in dictionary with key as day of the week.
    public Dictionary<string, WeekItem> WeekItems { get; set; } = new();

    // For Pinned Items - Includes
    public PinnedItems PinnedItems { get; set; } = new();

    // For Quick Access Folders
    public List<Folder> Folders { get; set; } = new();
}

public class FolderNote
{
    public string NoteId { get; set; } = "";
    public string Title { get; set; } = "";
}

public class WeekItem
{
    public List<Reminder> Reminders { get; set; } = new();

    public List<TaskItem> Tasks { get; set; } = new();
}

public class PinnedItems
{
    public List<Note> Notes { get; set; } = new();
    public List<Reminder> Reminders { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();

}


