using Microsoft.AspNetCore.Mvc;
using WorkOrderTracker.Api.Dtos;
using WorkOrderTracker.Api.Models;
using WorkOrderTracker.Api.Services;

namespace WorkOrderTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WorkOrdersController(IWorkOrderService service) : ControllerBase
{
    /// <summary>List work orders, optionally filtered by status and/or building.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkOrderDto>>> GetAll(
        [FromQuery] WorkOrderStatus? status, [FromQuery] int? buildingId) =>
        Ok(await service.GetAllAsync(status, buildingId));

    /// <summary>Get a single work order.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkOrderDto>> Get(int id)
    {
        var workOrder = await service.GetAsync(id);
        if (workOrder is null) return NotFound();
        return workOrder;
    }

    /// <summary>Create a work order.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkOrderDto>> Create(WorkOrderRequest request)
    {
        var result = await service.CreateAsync(request);
        if (result.Kind == ResultKind.Invalid) return Invalid(result.Errors);

        var created = result.Value!;
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Update a work order.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, WorkOrderRequest request)
    {
        var result = await service.UpdateAsync(id, request);
        if (result.Kind == ResultKind.NotFound) return NotFound();
        if (result.Kind == ResultKind.Invalid) return Invalid(result.Errors);
        return NoContent();
    }

    /// <summary>Delete a work order.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await service.DeleteAsync(id)) return NotFound();
        return NoContent();
    }

    private BadRequestObjectResult Invalid(IDictionary<string, string[]>? errors) =>
        BadRequest(new ValidationProblemDetails(errors ?? new Dictionary<string, string[]>()));
}
