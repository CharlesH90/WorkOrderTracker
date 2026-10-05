using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorkOrderTracker.Api.Data;
using WorkOrderTracker.Api.Dtos;
using WorkOrderTracker.Api.Models;

namespace WorkOrderTracker.Api.Services;

/// <summary>Work order queries and the business rules around changing them.</summary>
public class WorkOrderService(AppDbContext db, TimeProvider clock, IOptions<BusinessOptions> options) : IWorkOrderService
{
    private readonly TimeZoneInfo _businessZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);

    public async Task<IReadOnlyList<WorkOrderDto>> GetAllAsync(WorkOrderStatus? status, int? buildingId)
    {
        var query = db.WorkOrders
            .Include(w => w.Building)
            .Include(w => w.Technician)
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue) query = query.Where(w => w.Status == status.Value);
        if (buildingId.HasValue) query = query.Where(w => w.BuildingId == buildingId.Value);

        var items = await query.OrderByDescending(w => w.CreatedAt).ToListAsync();
        return items.Select(w => w.ToDto()).ToList();
    }

    public async Task<WorkOrderDto?> GetAsync(int id)
    {
        var workOrder = await LoadAsync(id);
        return workOrder?.ToDto();
    }

    public async Task<ServiceResult<WorkOrderDto>> CreateAsync(WorkOrderRequest request)
    {
        var errors = await ValidateAsync(request, isNew: true);
        if (errors.Count > 0) return ServiceResult<WorkOrderDto>.Invalid(errors);

        var workOrder = new WorkOrder { CreatedAt = clock.GetUtcNow().UtcDateTime };
        Apply(request, workOrder);
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        var created = (await LoadAsync(workOrder.Id))!;
        return ServiceResult<WorkOrderDto>.Success(created.ToDto());
    }

    public async Task<ServiceResult> UpdateAsync(int id, WorkOrderRequest request)
    {
        var workOrder = await db.WorkOrders.FindAsync(id);
        if (workOrder is null) return ServiceResult.NotFound();

        var errors = await ValidateAsync(request, isNew: false);
        if (errors.Count > 0) return ServiceResult.Invalid(errors);

        Apply(request, workOrder);
        await db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var workOrder = await db.WorkOrders.FindAsync(id);
        if (workOrder is null) return false;

        db.WorkOrders.Remove(workOrder);
        await db.SaveChangesAsync();
        return true;
    }

    private Task<WorkOrder?> LoadAsync(int id) =>
        db.WorkOrders
            .Include(w => w.Building)
            .Include(w => w.Technician)
            .FirstOrDefaultAsync(w => w.Id == id);

    /// <summary>Business rules that data annotations can't express. Keys match the request property names.</summary>
    private async Task<Dictionary<string, string[]>> ValidateAsync(WorkOrderRequest request, bool isNew)
    {
        var errors = new Dictionary<string, string[]>();

        if (!await db.Buildings.AnyAsync(b => b.Id == request.BuildingId))
            errors[nameof(request.BuildingId)] = ["Building does not exist."];

        if (request.TechnicianId.HasValue)
        {
            if (!await db.Technicians.AnyAsync(t => t.Id == request.TechnicianId.Value))
                errors[nameof(request.TechnicianId)] = ["Technician does not exist."];
        }
        else if (request.Status is WorkOrderStatus.InProgress or WorkOrderStatus.Completed)
        {
            errors[nameof(request.TechnicianId)] =
                [$"A technician must be assigned before a work order can be {request.Status}."];
        }

        if (isNew && request.DueDate.HasValue && request.DueDate.Value.Date < BusinessToday())
            errors[nameof(request.DueDate)] = ["Due date cannot be in the past."];

        return errors;
    }

    /// <summary>Today's calendar date in the business time zone, not the server's UTC date.</summary>
    private DateTime BusinessToday() => TimeZoneInfo.ConvertTime(clock.GetUtcNow(), _businessZone).Date;

    private void Apply(WorkOrderRequest request, WorkOrder workOrder)
    {
        // Stamp completion time on the transition into Completed; clear it if reopened.
        if (request.Status == WorkOrderStatus.Completed && workOrder.Status != WorkOrderStatus.Completed)
            workOrder.CompletedAt = clock.GetUtcNow().UtcDateTime;
        else if (request.Status != WorkOrderStatus.Completed)
            workOrder.CompletedAt = null;

        workOrder.Title = request.Title.Trim();
        workOrder.Description = request.Description;
        workOrder.Location = request.Location;
        workOrder.Priority = request.Priority;
        workOrder.Status = request.Status;
        workOrder.DueDate = request.DueDate;
        workOrder.BuildingId = request.BuildingId;
        workOrder.TechnicianId = request.TechnicianId;
    }
}
