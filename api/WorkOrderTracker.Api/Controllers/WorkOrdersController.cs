using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkOrderTracker.Api.Data;
using WorkOrderTracker.Api.Dtos;
using WorkOrderTracker.Api.Models;

namespace WorkOrderTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WorkOrdersController(AppDbContext db) : ControllerBase
{
    /// <summary>List work orders, optionally filtered by status and/or building.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkOrderDto>>> GetAll(
        [FromQuery] WorkOrderStatus? status, [FromQuery] int? buildingId)
    {
        var query = db.WorkOrders
            .Include(w => w.Building)
            .Include(w => w.Technician)
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue) query = query.Where(w => w.Status == status);
        if (buildingId.HasValue) query = query.Where(w => w.BuildingId == buildingId);

        var items = await query.OrderByDescending(w => w.CreatedAt).ToListAsync();
        return items.Select(w => w.ToDto()).ToList();
    }

    /// <summary>Get a single work order.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkOrderDto>> Get(int id)
    {
        var workOrder = await LoadAsync(id);
        return workOrder is null ? NotFound() : workOrder.ToDto();
    }

    /// <summary>Create a work order.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkOrderDto>> Create(WorkOrderRequest request)
    {
        await ValidateAsync(request, isNew: true);
        if (!ModelState.IsValid) return BadRequest(new ValidationProblemDetails(ModelState));

        var workOrder = new WorkOrder { CreatedAt = DateTime.UtcNow };
        Apply(request, workOrder);
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        var created = (await LoadAsync(workOrder.Id))!;
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created.ToDto());
    }

    /// <summary>Update a work order.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, WorkOrderRequest request)
    {
        var workOrder = await db.WorkOrders.FindAsync(id);
        if (workOrder is null) return NotFound();

        await ValidateAsync(request, isNew: false);
        if (!ModelState.IsValid) return BadRequest(new ValidationProblemDetails(ModelState));

        Apply(request, workOrder);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Delete a work order.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var workOrder = await db.WorkOrders.FindAsync(id);
        if (workOrder is null) return NotFound();

        db.WorkOrders.Remove(workOrder);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private Task<WorkOrder?> LoadAsync(int id) =>
        db.WorkOrders
            .Include(w => w.Building)
            .Include(w => w.Technician)
            .FirstOrDefaultAsync(w => w.Id == id);

    /// <summary>Business rules that data annotations can't express.</summary>
    private async Task ValidateAsync(WorkOrderRequest request, bool isNew)
    {
        if (!await db.Buildings.AnyAsync(b => b.Id == request.BuildingId))
            ModelState.AddModelError(nameof(request.BuildingId), "Building does not exist.");

        if (request.TechnicianId.HasValue && !await db.Technicians.AnyAsync(t => t.Id == request.TechnicianId))
            ModelState.AddModelError(nameof(request.TechnicianId), "Technician does not exist.");

        if (request.TechnicianId is null &&
            request.Status is WorkOrderStatus.InProgress or WorkOrderStatus.Completed)
            ModelState.AddModelError(nameof(request.TechnicianId),
                $"A technician must be assigned before a work order can be {request.Status}.");

        if (isNew && request.DueDate.HasValue && request.DueDate.Value.Date < DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(request.DueDate), "Due date cannot be in the past.");
    }

    private static void Apply(WorkOrderRequest request, WorkOrder workOrder)
    {
        // Stamp completion time on the transition into Completed; clear it if reopened.
        if (request.Status == WorkOrderStatus.Completed && workOrder.Status != WorkOrderStatus.Completed)
            workOrder.CompletedAt = DateTime.UtcNow;
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
