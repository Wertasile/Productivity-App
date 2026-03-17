
public class TaskBoardResponse
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public Dictionary<string, DateItems> Items { get; set; }
}

public class DateItems
{
    public List<TaskItem> Tasks { get; set; } = new();
    public List<Reminder> Reminders { get; set; } = new();

    public List<Note> Notes { get; set; } = new();
}

