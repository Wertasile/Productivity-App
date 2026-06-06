public class JournalItem
{
    public string Id { get; set; } = ""; // e.g. "2026-05-06"
    public string UserId { get; set; } = "";
    public string Content { get; set; } = "";
    // The journal day (date-only)
    public DateOnly Date { get; set; }
    // Stored/returned as ISO-8601 timestamps
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

