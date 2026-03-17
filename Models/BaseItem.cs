using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "EntityType")]
[JsonDerivedType(typeof(TaskItem), "TASK")]
[JsonDerivedType(typeof(Reminder), "REMINDER")]
public abstract class BaseItem
{
    public string Id { get; set; }

    public string UserId { get; set; }

    public string EntityType { get; set; }

    // DateTimeOFFSET Because your backend includes timezone (Z = UTC).
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

