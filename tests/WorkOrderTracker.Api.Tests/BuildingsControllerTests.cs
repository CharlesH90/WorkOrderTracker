using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkOrderTracker.Api.Controllers;
using WorkOrderTracker.Api.Dtos;
using WorkOrderTracker.Api.Models;
using WorkOrderTracker.Api.Tests.Support;

namespace WorkOrderTracker.Api.Tests;

public class BuildingsControllerTests
{
    // Seed: building 1 has work order 1 (InProgress), building 2 has work order 2 (Open).

    [Fact]
    public async Task GetAll_CountsOnlyUnfinishedWorkOrders()
    {
        using var test = new SqliteTestDb();
        await test.Db.WorkOrders.Where(w => w.Id == 2)
            .ExecuteUpdateAsync(s => s.SetProperty(w => w.Status, WorkOrderStatus.Cancelled));

        var result = await new BuildingsController(test.Db).GetAll();

        var counts = result.Value!.ToDictionary(b => b.Id, b => b.OpenWorkOrders);
        Assert.Equal(1, counts[1]);
        Assert.Equal(0, counts[2]);
    }

    [Fact]
    public async Task Create_Valid_Returns201()
    {
        using var test = new SqliteTestDb();

        var result = await new BuildingsController(test.Db)
            .Create(new BuildingRequest { Name = "Pine Court", Address = "9 Pine Ct" });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("Pine Court", Assert.IsType<BuildingDto>(created.Value).Name);
    }

    [Fact]
    public async Task Update_Missing_Returns404()
    {
        using var test = new SqliteTestDb();

        var result = await new BuildingsController(test.Db)
            .Update(999, new BuildingRequest { Name = "X", Address = "Y" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_WithWorkOrders_Returns409()
    {
        using var test = new SqliteTestDb();

        var result = await new BuildingsController(test.Db).Delete(1);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.True(await test.Db.Buildings.AnyAsync(b => b.Id == 1));
    }

    [Fact]
    public async Task Delete_WorkOrderAddedConcurrently_Returns409NotException()
    {
        // The building is empty when checked, but another request adds a work order before
        // our delete is saved. The Restrict foreign key rejects the delete; expect a 409.
        using var test = new SqliteTestDb();
        test.Db.Buildings.Add(new Building { Id = 3, Name = "Pine Court", Address = "9 Pine Ct" });
        await test.Db.SaveChangesAsync();
        test.Db.ChangeTracker.Clear();
        test.Interceptor.Sql =
            "INSERT INTO WorkOrders (Title, Priority, Status, CreatedAt, BuildingId) " +
            "VALUES ('Broken door', 'Low', 'Open', '2026-10-05 12:00:00', 3)";

        var result = await new BuildingsController(test.Db).Delete(3);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.True(await test.Db.Buildings.AnyAsync(b => b.Id == 3));
    }

    [Fact]
    public async Task Delete_EmptyBuilding_Returns204()
    {
        using var test = new SqliteTestDb();
        test.Db.Buildings.Add(new Building { Id = 3, Name = "Pine Court", Address = "9 Pine Ct" });
        await test.Db.SaveChangesAsync();
        test.Db.ChangeTracker.Clear();

        var result = await new BuildingsController(test.Db).Delete(3);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await test.Db.Buildings.AnyAsync(b => b.Id == 3));
    }
}
