
public class DashboardResponse
{
    // For Recent Notes
    public List<Note> RecentNotes { get; set; }

    // For Weekly Recap - Includes Note, Reminders and Tasks categorised in dictionary with key as day of the week.
    public Dictionary<string, WeekItem> WeekItems { get; set; }

    // For Pinned Items - Includes
    public PinnedItems PinnedItems { get; set; }

    // For Quick Access Folders
    public Dictionary<string, List<FolderNote>> Folders { get; set; }
}

public class FolderNote
{
    public string NoteId { get; set; }
    public string Title { get; set; }
}

public class WeekItem
{
    public List<Reminder> Reminders { get; set; }

    public List<Task> Tasks { get; set; }
}

public class PinnedItems
{
    public List<Note> Notes { get; set; }
    public List<Reminder> Reminders { get; set; }
    public List<Task> Tasks { get; set; }

}


