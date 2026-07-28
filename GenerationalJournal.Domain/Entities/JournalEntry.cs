namespace GenerationalJournal.Domain.Entities;

public class JournalEntry
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public Guid FamilyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Mood { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPrivate { get; set; }
    public string Tags { get; set; } = string.Empty;

    public User Author { get; set; } = null!;
    public Family Family { get; set; } = null!;
}
