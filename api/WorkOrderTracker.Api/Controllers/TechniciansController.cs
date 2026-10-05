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
            return DuplicateEmail(request);

        var tech = new Technician { Name = request.Name, Email = request.Email, Trade = request.Trade };
        db.Technicians.Add(tech);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // A concurrent request took the email after the check above; the unique index caught it.
            db.ChangeTracker.Clear();
            if (await db.Technicians.AnyAsync(t => t.Email == request.Email))
                return DuplicateEmail(request);
            throw;
        }

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
            return DuplicateEmail(request);

        tech.Name = request.Name;
        tech.Email = request.Email;
        tech.Trade = request.Trade;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (await db.Technicians.AnyAsync(t => t.Email == request.Email && t.Id != id))
                return DuplicateEmail(request);
            throw;
        }

        return NoContent();
    }

    /// <summary>
    /// Delete a technician. Open, on-hold and cancelled work orders become unassigned.
    /// Fails with 409 while the technician still has in-progress or completed work orders,
    /// because those must always have a technician.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var tech = await db.Technicians.FindAsync(id);
        if (tech is null) return NotFound();

        var needsTechnician = await db.WorkOrders.CountAsync(w =>
            w.TechnicianId == id &&
            (w.Status == WorkOrderStatus.InProgress || w.Status == WorkOrderStatus.Completed));
        if (needsTechnician > 0)
            return Conflict(new ProblemDetails
            {
                Title = "Technician has assigned work orders",
                Detail = $"{needsTechnician} in-progress or completed work order(s) still reference this technician. Reassign them first."
            });

        db.Technicians.Remove(tech);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private BadRequestObjectResult DuplicateEmail(TechnicianRequest request)
    {
        ModelState.AddModelError(nameof(request.Email), "A technician with this email already exists.");
        return BadRequest(new ValidationProblemDetails(ModelState));
    }
}
