using System.ComponentModel.DataAnnotations;

namespace WorkOrderTracker.Api.Dtos;

public record TechnicianDto(int Id, string Name, string Email, string Trade);

public class TechnicianRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Trade { get; set; } = string.Empty;
}
