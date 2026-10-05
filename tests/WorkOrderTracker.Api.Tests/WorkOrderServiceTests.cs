using Microsoft.EntityFrameworkCore;
using WorkOrderTracker.Api.Dtos;
using WorkOrderTracker.Api.Models;
using WorkOrderTracker.Api.Services;
using WorkOrderTracker.Api.Tests.Support;

namespace WorkOrderTracker.Api.Tests;

public class WorkOrderServiceTests
{
    // Seed: work order 1 = InProgress, building 1, technician 1. Work order 2 = Open, building 2, unassigned.
    private static readonly DateTimeOffset Noon = new(2026, 10, 5, 16, 0, 0, TimeSpan.Zero); // 12:00 in New York

    private static WorkOrderRequest ValidRequest() => new()
    {
        Title = "Replace hallway light",
        Location = "2nd floor hallway",
        Priority = WorkOrderPriority.Low,
        BuildingId = 1
    };

    [Fact]
    public async Task Create_Valid_PersistsWithClockTimestampAndBuildingName()
    {
        using var test = new SqliteTestDb();
        var service = test.CreateService(new FixedTimeProvider(Noon));

        var result = await service.CreateAsync(ValidRequest());

        Assert.Equal(ResultKind.Success, result.Kind);
        Assert.Equal("Harbor View Apartments", result.Value!.BuildingName);
        Assert.Equal(WorkOrderStatus.Open, result.Value.Status);
        Assert.Equal(Noon.UtcDateTime, result.Value.CreatedAt);
        Assert.Equal(3, await test.Db.WorkOrders.CountAsync());
    }

    [Fact]
    public async Task Create_TrimsTitle()
    {
        using var test = new SqliteTestDb();
        var request = ValidRequest();
        request.Title = "   Replace hallway light  ";

        var result = await test.CreateService().CreateAsync(request);

        Assert.Equal("Replace hallway light", result.Value!.Title);
    }

    [Fact]
    public async Task Create_UnknownBuilding_IsInvalid()
    {
        using var test = new SqliteTestDb();
        var request = ValidRequest();
        request.BuildingId = 999;

        var result = await test.CreateService().CreateAsync(request);

        Assert.Equal(ResultKind.Invalid, result.Kind);
        Assert.Contains(nameof(WorkOrderRequest.BuildingId), result.Errors!.Keys);
        Assert.Equal(2, await test.Db.WorkOrders.CountAsync());
    }

    [Fact]
    public async Task Create_UnknownTechnician_IsInvalid()
    {
        using var test = new SqliteTestDb();
        var request = ValidRequest();
        request.TechnicianId = 999;

        var result = await test.CreateService().CreateAsync(request);

        Assert.Equal(ResultKind.Invalid, result.Kind);
        Assert.Contains(nameof(WorkOrderRequest.TechnicianId), result.Errors!.Keys);
    }

    [Theory]
    [InlineData(WorkOrderStatus.InProgress)]
    [InlineData(WorkOrderStatus.Completed)]
    public async Task Create_ActiveStatusWithoutTechnician_IsInvalid(WorkOrderStatus status)
    {
        using var test = new SqliteTestDb();
        var request = ValidRequest();
        request.Status = status;

        var result = await test.CreateService().CreateAsync(request);

        Assert.Equal(ResultKind.Invalid, result.Kind);
        Assert.Contains(nameof(WorkOrderRequest.TechnicianId), result.Errors!.Keys);
    }

    [Theory]
    [InlineData(WorkOrderStatus.Open)]
    [InlineData(WorkOrderStatus.OnHold)]
    [InlineData(WorkOrderStatus.Cancelled)]
    public async Task Create_NonActiveStatusWithoutTechnician_IsAllowed(WorkOrderStatus status)
    {
        using var test = new SqliteTestDb();
        var request = ValidRequest();
        request.Status = status;

        var result = await test.CreateService().CreateAsync(request);

        Assert.Equal(ResultKind.Success, result.Kind);
    }

    [Fact]
    public async Task Create_DueDateYesterday_IsInvalid()
    {
        using var test = new SqliteTestDb();
        var request = ValidRequest();
        request.DueDate = new DateTime(2026, 10, 4);

        var result = await test.CreateService(new FixedTimeProvider(Noon)).CreateAsync(request);

        Assert.Equal(ResultKind.Invalid, result.Kind);
        Assert.Contains(nameof(WorkOrderRequest.DueDate), result.Errors!.Keys);
    }

    [Fact]
    public async Task Create_DueDateToday_IsAllowed()
    {
        using var test = new SqliteTestDb();
        var request = ValidRequest();
        request.DueDate = new DateTime(2026, 10, 5);

        var result = await test.CreateService(new FixedTimeProvider(Noon)).CreateAsync(request);

        Assert.Equal(ResultKind.Success, result.Kind);
    }

    [Fact]
    public async Task Create_DueDateToday_IsAllowedInTheEveningInNewYork()
    {
        // 9:30 PM on Oct 5 in New York is already 01:30 UTC on Oct 6. The rule must use the
        // business time zone, otherwise "today" is wrongly rejected after 8 PM Eastern.
        using var test = new SqliteTestDb();
        var request = ValidRequest();
        request.DueDate = new DateTime(2026, 10, 5);
        var lateEvening = new FixedTimeProvider(new DateTimeOffset(2026, 10, 6, 1, 30, 0, TimeSpan.Zero));

        var result = await test.CreateService(lateEvening).CreateAsync(request);

        Assert.Equal(ResultKind.Success, result.Kind);
    }

    [Fact]
    public async Task Update_PastDueDateOnExistingOrder_IsAllowed()
    {
        // The past-date rule only applies when creating; an overdue order can still be edited.
        using var test = new SqliteTestDb();
        var request = ValidRequest();
        request.BuildingId = 1;
        request.TechnicianId = 1;
        request.Status = WorkOrderStatus.InProgress;
        request.DueDate = new DateTime(2026, 9, 1);

        var result = await test.CreateService(new FixedTimeProvider(Noon)).UpdateAsync(1, request);

        Assert.Equal(ResultKind.Success, result.Kind);
    }

    [Fact]
    public async Task Update_Missing_IsNotFound()
    {
        using var test = new SqliteTestDb();

        var result = await test.CreateService().UpdateAsync(999, ValidRequest());

        Assert.Equal(ResultKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task Update_CompleteWithoutTechnician_IsInvalid()
    {
        using var test = new SqliteTestDb();
        var request = ValidRequest();
        request.Status = WorkOrderStatus.Completed;

        var result = await test.CreateService().UpdateAsync(2, request);

        Assert.Equal(ResultKind.Invalid, result.Kind);
        Assert.Contains(nameof(WorkOrderRequest.TechnicianId), result.Errors!.Keys);
    }

    [Fact]
    public async Task Update_ToCompleted_StampsCompletedAt()
    {
        using var test = new SqliteTestDb();
        var request = ValidRequest();
        request.Status = WorkOrderStatus.Completed;
        request.TechnicianId = 1;

        var result = await test.CreateService(new FixedTimeProvider(Noon)).UpdateAsync(1, request);

        Assert.Equal(ResultKind.Success, result.Kind);
        var saved = await test.Db.WorkOrders.AsNoTracking().FirstAsync(w => w.Id == 1);
        Assert.Equal(Noon.UtcDateTime, saved.CompletedAt);
    }

    [Fact]
    public async Task Update_AlreadyCompleted_KeepsOriginalCompletedAt()
    {
        using var test = new SqliteTestDb();
        var request = ValidRequest();
        request.Status = WorkOrderStatus.Completed;
        request.TechnicianId = 1;
        await test.CreateService(new FixedTimeProvider(Noon)).UpdateAsync(1, request);

        request.Description = "Edited after completion";
        await test.CreateService(new FixedTimeProvider(Noon.AddDays(3))).UpdateAsync(1, request);

        var saved = await test.Db.WorkOrders.AsNoTracking().FirstAsync(w => w.Id == 1);
        Assert.Equal(Noon.UtcDateTime, saved.CompletedAt);
        Assert.Equal("Edited after completion", saved.Description);
    }

    [Fact]
    public async Task Update_ReopeningCompletedOrder_ClearsCompletedAt()
    {
        using var test = new SqliteTestDb();
        var service = test.CreateService(new FixedTimeProvider(Noon));
        var request = ValidRequest();
        request.Status = WorkOrderStatus.Completed;
        request.TechnicianId = 1;
        await service.UpdateAsync(1, request);

        request.Status = WorkOrderStatus.Open;
        await service.UpdateAsync(1, request);

        var saved = await test.Db.WorkOrders.AsNoTracking().FirstAsync(w => w.Id == 1);
        Assert.Null(saved.CompletedAt);
    }

    [Fact]
    public async Task GetAll_FilterByStatus_ReturnsOnlyMatching()
    {
        using var test = new SqliteTestDb();

        var items = await test.CreateService().GetAllAsync(WorkOrderStatus.Open, buildingId: null);

        var only = Assert.Single(items);
        Assert.Equal(2, only.Id);
    }

    [Fact]
    public async Task GetAll_FilterByBuilding_ReturnsOnlyMatching()
    {
        using var test = new SqliteTestDb();

        var items = await test.CreateService().GetAllAsync(status: null, buildingId: 1);

        var only = Assert.Single(items);
        Assert.Equal(1, only.Id);
    }

    [Fact]
    public async Task GetAll_NoFilters_ReturnsNewestFirst()
    {
        using var test = new SqliteTestDb();

        var items = await test.CreateService().GetAllAsync(null, null);

        Assert.Equal([2, 1], items.Select(w => w.Id));
    }

    [Fact]
    public async Task Get_Missing_ReturnsNull()
    {
        using var test = new SqliteTestDb();

        Assert.Null(await test.CreateService().GetAsync(999));
    }

    [Fact]
    public async Task Delete_Existing_RemovesIt()
    {
        using var test = new SqliteTestDb();

        Assert.True(await test.CreateService().DeleteAsync(2));
        Assert.Equal(1, await test.Db.WorkOrders.CountAsync());
    }

    [Fact]
    public async Task Delete_Missing_ReturnsFalse()
    {
        using var test = new SqliteTestDb();

        Assert.False(await test.CreateService().DeleteAsync(999));
    }
}
