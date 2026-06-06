public class Project
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<Phase> Phases { get; set; } = new List<Phase>();
    public List<Note> Notes { get; set; } = new List<Note>();
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Phase
{
    public string Id { get; set; } = "";   
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public List<Note> Notes { get; set; } = new List<Note>();
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int TaskCount {get; set;} = 0;
}