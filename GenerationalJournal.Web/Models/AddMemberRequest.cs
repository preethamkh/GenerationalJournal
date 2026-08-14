using System.ComponentModel.DataAnnotations;

namespace GenerationalJournal.Web.Models;

public class AddMemberRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Role { get; set; } = "Member";

    [MaxLength(256)]
    public string RelationshipDescription { get; set; } = string.Empty;
}
