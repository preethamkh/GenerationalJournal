namespace GenerationalJournal.Application.DTOs.Media;

public class MediaResponse
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }
    public Guid FamilyId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
