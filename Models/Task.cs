
using System.Text.Json.Serialization;

public class TaskItem : BaseItem
{
    //public string Id { get; set; }

    //public string UserId { get; set; }

    // Calendar-only (null for phase tasks unless you add subtasks later)
    public string? parentTaskId { get; set; } = null;// if task is subtask

    public string Title { get; set; }

    public string EntityType { get; set; } = "TASK";

    public bool IsPhaseTask { get; set; }
    // Phase-only (null for calendar tasks)
    public string? ProjectId { get; set; }
    public string? PhaseId { get; set; }
    

    public string Description { get; set; }

    public int Progress { get; set; } = 0;

    public int Priority { get; set; } = 0;

    public string Status { get; set; } = "Not Started";

    public DateTime? CompletionDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Backend can return null for Start/End; treat null as default(DateTime) to avoid breaking deserialization.
    [JsonConverter(typeof(NullToDefaultDateTimeConverter))]
    public DateTime Start { get; set; }

    [JsonConverter(typeof(NullToDefaultDateTimeConverter))]
    public DateTime End { get; set; }
}

