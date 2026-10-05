using Microsoft.AspNetCore.Mvc;
using WorkOrderTracker.Api.Controllers;
using WorkOrderTracker.Api.Dtos;
using WorkOrderTracker.Api.Models;
using WorkOrderTracker.Api.Tests.Support;

namespace WorkOrderTracker.Api.Tests;

/// <summary>The controller only maps service results to HTTP results; the rules are tested in WorkOrderServiceTests.</summary>
public class WorkOrdersControllerTests
{
    private static WorkOrderRequest ValidRequest() => new()
    {
        Title = "Replace hallway light",
        Priority = WorkOrderPriority.Low,
        BuildingId = 1
    };

    private static WorkOrdersController CreateController(SqliteTestDb test) => new(test.CreateService());

    [Fact]
    public async Task GetAll_ReturnsOkWithItems()
    {
        using var test = new SqliteTestDb();

        var result = await CreateController(test).GetAll(null, null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(2, Assert.IsAssignableFrom<IEnumerable<WorkOrderDto>>(ok.Value).Count());
    }

    [Fact]
    public async Task Get_Missing_Returns404()
    {
        using var test = new SqliteTestDb();

        var result = await CreateController(test).Get(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_Valid_Returns201WithLocation()
    {
        using var test = new SqliteTestDb();

        var result = await CreateController(test).Create(ValidRequest());

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(WorkOrdersController.Get), created.ActionName);
        Assert.IsType<WorkOrderDto>(created.Value);
    }

    [Fact]
    public async Task Create_UnknownBuilding_Returns400WithValidationProblem()
    {
        using var test = new SqliteTestDb();
        var request = ValidRequest();
        request.BuildingId = 999;

        var result = await CreateController(test).Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Contains(nameof(WorkOrderRequest.BuildingId), problem.Errors.Keys);
    }

    [Fact]
    public async Task Update_Valid_Returns204()
    {
        using var test = new SqliteTestDb();

        var result = await CreateController(test).Update(2, ValidRequest());

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_Missing_Returns404()
    {
        using var test = new SqliteTestDb();

        var result = await CreateController(test).Update(999, ValidRequest());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_CompleteWithoutTechnician_Returns400()
    {
        using var test = new SqliteTestDb();
        var request = ValidRequest();
        request.Status = WorkOrderStatus.Completed;

        var result = await CreateController(test).Update(2, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Contains(nameof(WorkOrderRequest.TechnicianId), problem.Errors.Keys);
    }

    [Fact]
    public async Task Delete_Existing_Returns204()
    {
        using var test = new SqliteTestDb();

        Assert.IsType<NoContentResult>(await CreateController(test).Delete(2));
    }

    [Fact]
    public async Task Delete_Missing_Returns404()
    {
        using var test = new SqliteTestDb();

        Assert.IsType<NotFoundResult>(await CreateController(test).Delete(999));
    }
}
