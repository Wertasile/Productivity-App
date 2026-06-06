
//public class CalendarResponse
//{
//    public Dictionary<string, CalendarItem> Calendar { get; set; } = new();
//}

// the object literally is the dictionary, so we can just make it inherit from the dictionary instead of having a property for it
// it is not a property / inside the returned object, it is the returned object, so we can just make the class itself be the dictionary instead of having a property for it
public class CalendarResponse : Dictionary<string, CalendarItem>
{
}

public class DayResponse
{
    public List<Note> Notes { get; set; }

    public List<Reminder> Reminders { get; set; }

    public List<TaskItem> Tasks { get; set; }
}

public class CalendarItem
{
    public List<Note> Notes { get; set; } = new();
    public List<Reminder> Reminders { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();
}
