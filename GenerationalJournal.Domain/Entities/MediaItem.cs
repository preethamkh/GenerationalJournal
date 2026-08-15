namespace GenerationalJournal.Domain.Entities;

public class MediaItem
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }
    public Guid FamilyId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public JournalEntry Entry { get; set; } = null!;
}
