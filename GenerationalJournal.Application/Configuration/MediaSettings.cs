namespace GenerationalJournal.Application.Configuration;

public class MediaSettings
{
    public string StorageRootPath { get; set; } = string.Empty;
    public long MaxFileSizeBytes { get; set; } = 100L * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } = { ".jpg", ".png", ".gif", ".mp4", ".mov" };
}
