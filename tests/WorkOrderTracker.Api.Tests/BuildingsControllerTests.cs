using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkOrderTracker.Api.Controllers;
using WorkOrderTracker.Api.Data;

namespace WorkOrderTracker.Api.Tests;

public class BuildingsControllerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Delete_BuildingWithWorkOrders_Returns409()
    {
        using var db = CreateDb();
        var controller = new BuildingsController(db);

        var result = await controller.Delete(1);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(await db.Buildings.FindAsync(1));
    }
}
