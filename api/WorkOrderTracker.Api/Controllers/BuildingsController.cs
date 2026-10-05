using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkOrderTracker.Api.Data;
using WorkOrderTracker.Api.Dtos;
using WorkOrderTracker.Api.Models;

namespace WorkOrderTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BuildingsController(AppDbContext db) : ControllerBase
{
    /// <summary>List all buildings with their count of unfinished work orders.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BuildingDto>>> GetAll() =>
        await db.Buildings
            .OrderBy(b => b.Name)
            .Select(b => new BuildingDto(b.Id, b.Name, b.Address,
                b.WorkOrders.Count(w => w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Cancelled)))
            .ToListAsync();

    /// <summary>Get a single building.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BuildingDto>> Get(int id)
    {
        var dto = await db.Buildings
            .Where(b => b.Id == id)
            .Select(b => new BuildingDto(b.Id, b.Name, b.Address,
                b.WorkOrders.Count(w => w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Cancelled)))
            .FirstOrDefaultAsync();

        return dto is null ? NotFound() : dto;
    }

    /// <summary>Create a building.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BuildingDto>> Create(BuildingRequest request)
    {
        var building = new Building { Name = request.Name, Address = request.Address };
        db.Buildings.Add(building);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = building.Id },
            new BuildingDto(building.Id, building.Name, building.Address, 0));
    }

    /// <summary>Update a building.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, BuildingRequest request)
    {
        var building = await db.Buildings.FindAsync(id);
        if (building is null) return NotFound();

        building.Name = request.Name;
        building.Address = request.Address;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Delete a building. Fails with 409 if it still has work orders.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var building = await db.Buildings.FindAsync(id);
        if (building is null) return NotFound();

        if (await db.WorkOrders.AnyAsync(w => w.BuildingId == id))
            return Conflict(new ProblemDetails
            {
                Title = "Building has work orders",
                Detail = "Delete or reassign this building's work orders first."
            });

        db.Buildings.Remove(building);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
