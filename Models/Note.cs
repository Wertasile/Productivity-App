public class Note : BaseItem
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string? FolderId { get; set; }  // null = root
    public string EntityType { get; set; } = "NOTE";

    public string Title { get; set; } = "";
    public string Content { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}