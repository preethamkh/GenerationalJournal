namespace GenerationalJournal.Application.DTOs.Journal;

using System.ComponentModel.DataAnnotations;

public class UpdateJournalEntryRequest
{
    [Required]
    [MaxLength(512)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Mood { get; set; } = string.Empty;

    public DateTime? EntryDate { get; set; }

    public bool IsPrivate { get; set; }

    [MaxLength(1024)]
    public string Tags { get; set; } = string.Empty;
}
