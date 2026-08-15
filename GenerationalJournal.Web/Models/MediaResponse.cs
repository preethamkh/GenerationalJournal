namespace GenerationalJournal.Web.Models;

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

    public string Preview { get; set; } = string.Empty;

    public string DisplaySize => FileSizeBytes switch
    {
        >= 1024 * 1024 => $"{FileSizeBytes / 1024.0 / 1024.0:0.0} MB",
        >= 1024 => $"{FileSizeBytes / 1024.0:0.0} KB",
        _ => $"{FileSizeBytes} B"
    };
}
