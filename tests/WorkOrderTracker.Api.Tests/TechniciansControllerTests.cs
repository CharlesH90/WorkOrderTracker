using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkOrderTracker.Api.Controllers;
using WorkOrderTracker.Api.Dtos;
using WorkOrderTracker.Api.Models;
using WorkOrderTracker.Api.Tests.Support;

namespace WorkOrderTracker.Api.Tests;

public class TechniciansControllerTests
{
    // Seed: technician 1 (Sam) has work order 1 InProgress. Technician 2 (Jordan) has none.
    private static TechnicianRequest NewTech(string email = "casey.nguyen@example.com") => new()
    {
        Name = "Casey Nguyen",
        Email = email,
        Trade = "Electrical"
    };

    [Fact]
    public async Task GetAll_ReturnsSeededTechniciansByName()
    {
        using var test = new SqliteTestDb();

        var result = await new TechniciansController(test.Db).GetAll();

        Assert.Equal(["Jordan Lee", "Sam Rivera"], result.Value!.Select(t => t.Name));
    }

    [Fact]
    public async Task Create_Valid_Returns201()
    {
        using var test = new SqliteTestDb();

        var result = await new TechniciansController(test.Db).Create(NewTech());

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("casey.nguyen@example.com", Assert.IsType<TechnicianDto>(created.Value).Email);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns400()
    {
        using var test = new SqliteTestDb();

        var result = await new TechniciansController(test.Db).Create(NewTech("sam.rivera@example.com"));

        AssertEmailError(result.Result);
    }

    [Fact]
    public async Task Create_EmailTakenByConcurrentRequest_Returns400NotException()
    {
        // Another request inserts the same email after our duplicate check but before our save.
        // The unique index rejects our insert; the controller must turn that into a 400.
        using var test = new SqliteTestDb();
        test.Interceptor.Sql =
            "INSERT INTO Technicians (Name, Email, Trade) VALUES ('Other', 'casey.nguyen@example.com', 'HVAC')";

        var result = await new TechniciansController(test.Db).Create(NewTech());

        AssertEmailError(result.Result);
        Assert.Equal(1, await test.Db.Technicians.CountAsync(t => t.Email == "casey.nguyen@example.com"));
    }

    [Fact]
    public async Task Update_EmailOfAnotherTechnician_Returns400()
    {
        using var test = new SqliteTestDb();

        var result = await new TechniciansController(test.Db).Update(2, NewTech("sam.rivera@example.com"));

        AssertEmailError(result);
    }

    [Fact]
    public async Task Update_KeepingOwnEmail_Returns204()
    {
        using var test = new SqliteTestDb();
        var request = NewTech("jordan.lee@example.com");

        var result = await new TechniciansController(test.Db).Update(2, request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_Missing_Returns404()
    {
        using var test = new SqliteTestDb();

        Assert.IsType<NotFoundResult>(await new TechniciansController(test.Db).Update(999, NewTech()));
    }

    [Theory]
    [InlineData(WorkOrderStatus.InProgress)]
    [InlineData(WorkOrderStatus.Completed)]
    public async Task Delete_WithWorkThatNeedsATechnician_Returns409(WorkOrderStatus status)
    {
        using var test = new SqliteTestDb();
        await test.Db.WorkOrders.Where(w => w.Id == 1).ExecuteUpdateAsync(s => s.SetProperty(w => w.Status, status));

        var result = await new TechniciansController(test.Db).Delete(1);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.True(await test.Db.Technicians.AnyAsync(t => t.Id == 1));
    }

    [Fact]
    public async Task Delete_WithOnlyOpenWork_UnassignsIt()
    {
        // Give Jordan the open work order, then delete Jordan. The SetNull foreign key
        // (enforced by SQLite, not just by EF) should leave the order unassigned.
        using var test = new SqliteTestDb();
        await test.Db.WorkOrders.Where(w => w.Id == 2).ExecuteUpdateAsync(s => s.SetProperty(w => w.TechnicianId, 2));

        var result = await new TechniciansController(test.Db).Delete(2);

        Assert.IsType<NoContentResult>(result);
        var order = await test.Db.WorkOrders.AsNoTracking().FirstAsync(w => w.Id == 2);
        Assert.Null(order.TechnicianId);
    }

    [Fact]
    public async Task Delete_Missing_Returns404()
    {
        using var test = new SqliteTestDb();

        Assert.IsType<NotFoundResult>(await new TechniciansController(test.Db).Delete(999));
    }

    private static void AssertEmailError(IActionResult? result)
    {
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Contains(nameof(TechnicianRequest.Email), problem.Errors.Keys);
    }
}
