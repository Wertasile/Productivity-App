
public class TaskItem : BaseItem
{
    public string Id { get; set; }

    public string UserId { get; set; }

    public string parentTaskId { get; set; } = null;// if task is subtask

    public string Title { get; set; }

    public string EntityType { get; set; } = "TASK";

    public string Description { get; set; }

    public int Progress { get; set; } = 0;

    public bool IsCompleted { get; set; } = false;

    public DateTime? CompletionDate { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

