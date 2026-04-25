public class LocalAppState
{
    public List<Folder> Folders { get; set; } = new();

    public List<Note> Notes { get; set; } = new();

    public List<TaskItem> Tasks { get; set; } = new();

    public List<Reminder> Reminders { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
