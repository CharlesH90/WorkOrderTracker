using System.ComponentModel.DataAnnotations;
using WorkOrderTracker.Api.Models;

namespace WorkOrderTracker.Api.Dtos;

public record WorkOrderDto(
    int Id,
    string Title,
    string? Description,
    string? Location,
    WorkOrderPriority Priority,
    WorkOrderStatus Status,
    DateTime CreatedAt,
    DateTime? DueDate,
    DateTime? CompletedAt,
    int BuildingId,
    string BuildingName,
    int? TechnicianId,
    string? TechnicianName);

/// <summary>Body for creating or updating a work order.</summary>
public class WorkOrderRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    [EnumDataType(typeof(WorkOrderPriority))]
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;

    [EnumDataType(typeof(WorkOrderStatus))]
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Open;

    public DateTime? DueDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A building is required.")]
    public int BuildingId { get; set; }

    public int? TechnicianId { get; set; }
}
