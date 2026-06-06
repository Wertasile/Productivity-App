
public class FolderContent
{
    public FolderMetadata Metadata { get; set; } = new ();
    public List<Note> Notes {  get; set; } = new ();

    public List<Folder> Folders { get; set; } = new ();
}

public class FolderMetadata
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string? ParentFolderId { get; set; }  // null = root
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsQuickAccess { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

