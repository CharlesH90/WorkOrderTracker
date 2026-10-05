using System.ComponentModel.DataAnnotations;

namespace WorkOrderTracker.Api.Dtos;

public record BuildingDto(int Id, string Name, string Address, int OpenWorkOrders);

public class BuildingRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Address { get; set; } = string.Empty;
}
