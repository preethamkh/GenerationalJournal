namespace GenerationalJournal.Web.Models;

public class JournalEntryResponse
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public Guid FamilyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Mood { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsPrivate { get; set; }
    public string Tags { get; set; } = string.Empty;
    public string AuthorFirstName { get; set; } = string.Empty;
    public string AuthorLastName { get; set; } = string.Empty;

    public string AuthorName => $"{AuthorFirstName} {AuthorLastName}".Trim();

    public List<string> TagList => string.IsNullOrWhiteSpace(Tags)
        ? new List<string>()
        : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
