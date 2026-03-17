public class Folder : BaseItem
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string? ParentFolderId { get; set; }  // null = root

    public string Path { get; set; } = "";  // e.g. "/folder1/folder2/"
    public string Name { get; set; } = "";
    public string EntityType { get; set; } = "FOLDER";
    public string? Description { get; set; }
    public bool IsQuickAccess { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}