namespace GenerationalJournal.Web.Models;

public class FamilyMemberResponse
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public string RelationshipDescription { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();
}
