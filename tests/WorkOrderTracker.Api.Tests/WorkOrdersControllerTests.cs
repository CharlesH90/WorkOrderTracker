using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkOrderTracker.Api.Controllers;
using WorkOrderTracker.Api.Data;
using WorkOrderTracker.Api.Dtos;
using WorkOrderTracker.Api.Models;

namespace WorkOrderTracker.Api.Tests;

public class WorkOrdersControllerTests
{
    // Each test gets its own in-memory database. HasData seeds (2 buildings, 2 technicians,
    // 2 work orders) are applied by EnsureCreated.
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static WorkOrderRequest ValidRequest() => new()
    {
        Title = "Replace hallway light",
        Location = "2nd floor hallway",
        Priority = WorkOrderPriority.Low,
        BuildingId = 1
    };

    [Fact]
    public async Task Create_ValidRequest_Returns201AndPersists()
    {
        using var db = CreateDb();
        var controller = new WorkOrdersController(db);

        var result = await controller.Create(ValidRequest());

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<WorkOrderDto>(created.Value);
        Assert.Equal("Harbor View Apartments", dto.BuildingName);
        Assert.Equal(WorkOrderStatus.Open, dto.Status);
        Assert.Equal(3, await db.WorkOrders.CountAsync());
    }

    [Fact]
    public async Task Create_UnknownBuilding_ReturnsValidationError()
    {
        using var db = CreateDb();
        var controller = new WorkOrdersController(db);
        var request = ValidRequest();
        request.BuildingId = 999;

        var result = await controller.Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Contains(nameof(WorkOrderRequest.BuildingId), problem.Errors.Keys);
    }

    [Fact]
    public async Task Update_CompleteWithoutTechnician_ReturnsValidationError()
    {
        using var db = CreateDb();
        var controller = new WorkOrdersController(db);
        var request = ValidRequest();
        request.Status = WorkOrderStatus.Completed;
        request.TechnicianId = null;

        var result = await controller.Update(2, request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Contains(nameof(WorkOrderRequest.TechnicianId), problem.Errors.Keys);
    }

    [Fact]
    public async Task Update_ToCompleted_StampsCompletedAt()
    {
        using var db = CreateDb();
        var controller = new WorkOrdersController(db);
        var request = ValidRequest();
        request.Status = WorkOrderStatus.Completed;
        request.TechnicianId = 1;

        var result = await controller.Update(1, request);

        Assert.IsType<NoContentResult>(result);
        var saved = await db.WorkOrders.FindAsync(1);
        Assert.NotNull(saved!.CompletedAt);
    }

    [Fact]
    public async Task GetAll_FilterByStatus_ReturnsOnlyMatching()
    {
        using var db = CreateDb();
        var controller = new WorkOrdersController(db);

        var result = await controller.GetAll(WorkOrderStatus.Open, buildingId: null);

        var items = Assert.IsAssignableFrom<IEnumerable<WorkOrderDto>>(result.Value);
        Assert.All(items, w => Assert.Equal(WorkOrderStatus.Open, w.Status));
        Assert.Single(items);
    }

    [Fact]
    public async Task Delete_Missing_Returns404()
    {
        using var db = CreateDb();
        var controller = new WorkOrdersController(db);

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }
}
