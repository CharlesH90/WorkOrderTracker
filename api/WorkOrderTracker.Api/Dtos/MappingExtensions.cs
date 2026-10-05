using WorkOrderTracker.Api.Models;

namespace WorkOrderTracker.Api.Dtos;

public static class MappingExtensions
{
    public static TechnicianDto ToDto(this Technician t) => new(t.Id, t.Name, t.Email, t.Trade);

    public static WorkOrderDto ToDto(this WorkOrder w) => new(
        w.Id, w.Title, w.Description, w.Location, w.Priority, w.Status,
        w.CreatedAt, w.DueDate, w.CompletedAt,
        w.BuildingId, w.Building.Name,
        w.TechnicianId, w.Technician?.Name);
}
