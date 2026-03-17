
public class Reminder : BaseItem
{
    public string Id { get; set; }

    public string UserId { get; set; }

    public string Name { get; set; }

    public string EntityType { get; set; } = "REMINDER";

    public string Description { get; set; }

    public bool IsAcknowledged { get; set; }

    public DateTime ReminderDate { get; set; }


    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;


}

