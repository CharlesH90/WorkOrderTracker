namespace WorkOrderTracker.Api.Models;

public enum WorkOrderPriority
{
    Low,
    Medium,
    High,
    Urgent
}

public enum WorkOrderStatus
{
    Open,
    InProgress,
    OnHold,
    Completed,
    Cancelled
}

public class WorkOrder
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Where in the building, e.g. "Unit 4B" or "Roof".</summary>
    public string? Location { get; set; }

    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Open;

    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int BuildingId { get; set; }
    public Building Building { get; set; } = null!;

    public int? TechnicianId { get; set; }
    public Technician? Technician { get; set; }
}
