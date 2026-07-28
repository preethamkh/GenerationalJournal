namespace GenerationalJournal.Domain.Entities;

public class FamilyMember
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Member";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public string RelationshipDescription { get; set; } = string.Empty;

    public Family Family { get; set; } = null!;
    public User User { get; set; } = null!;
}
