namespace GenerationalJournal.Application.DTOs.Family;

using System.ComponentModel.DataAnnotations;

public class CreateFamilyRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;
}
