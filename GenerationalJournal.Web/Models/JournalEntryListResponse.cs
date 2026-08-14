namespace GenerationalJournal.Web.Models;

public class JournalEntryListResponse
{
    public List<JournalEntryResponse> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
