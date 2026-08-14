using System.ComponentModel.DataAnnotations;

namespace GenerationalJournal.Web.Models;

public class CreateFamilyRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;
}
