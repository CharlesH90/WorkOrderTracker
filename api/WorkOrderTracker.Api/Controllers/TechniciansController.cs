using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkOrderTracker.Api.Data;
using WorkOrderTracker.Api.Dtos;
using WorkOrderTracker.Api.Models;

namespace WorkOrderTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TechniciansController(AppDbContext db) : ControllerBase
{
    /// <summary>List all technicians.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TechnicianDto>>> GetAll() =>
        (await db.Technicians.OrderBy(t => t.Name).ToListAsync()).Select(t => t.ToDto()).ToList();

    /// <summary>Get a single technician.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TechnicianDto>> Get(int id)
    {
        var tech = await db.Technicians.FindAsync(id);
        return tech is null ? NotFound() : tech.ToDto();
    }

    /// <summary>Create a technician. Email must be unique.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TechnicianDto>> Create(TechnicianRequest request)
    {
        if (await db.Technicians.AnyAsync(t => t.Email == request.Email))
        {
            ModelState.AddModelError(nameof(request.Email), "A technician with this email already exists.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        var tech = new Technician { Name = request.Name, Email = request.Email, Trade = request.Trade };
        db.Technicians.Add(tech);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = tech.Id }, tech.ToDto());
    }

    /// <summary>Update a technician.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, TechnicianRequest request)
    {
        var tech = await db.Technicians.FindAsync(id);
        if (tech is null) return NotFound();

        if (await db.Technicians.AnyAsync(t => t.Email == request.Email && t.Id != id))
        {
            ModelState.AddModelError(nameof(request.Email), "A technician with this email already exists.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        tech.Name = request.Name;
        tech.Email = request.Email;
        tech.Trade = request.Trade;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Delete a technician. Their work orders become unassigned.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var tech = await db.Technicians.FindAsync(id);
        if (tech is null) return NotFound();

        db.Technicians.Remove(tech);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
