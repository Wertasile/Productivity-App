public class TreeView
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<minFolder> Folders { get; set; }
    public List<minNote> Notes { get; set; }
}


public class minFolder
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ParentFolderId { get; set; }
    public List<minFolder> Folders { get; set; }
    public List<minNote> Notes{ get; set; }
}

public class minNote
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string FolderId { get; set; }
}


