public class CalendarEvent
{
    public string Id { get; set; }

    public string Title { get; set; }

    public DateTime Start { get; set; }

    public DateTime End { get; set; }

    public string Type { get; set; } // TASK / REMINDER / NOTE

    public string SourceId { get; set; }
}


