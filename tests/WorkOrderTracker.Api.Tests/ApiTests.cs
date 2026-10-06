using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WorkOrderTracker.Api.Dtos;
using WorkOrderTracker.Api.Models;
using WorkOrderTracker.Api.Services;
using WorkOrderTracker.Api.Tests.Support;

namespace WorkOrderTracker.Api.Tests;

/// <summary>
/// Real HTTP requests through the full pipeline: routing, model binding, [ApiController]
/// validation of the DataAnnotations, JSON enum handling and the global error handler.
/// These tests share one database, so they only read or send requests that get rejected.
/// </summary>
public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostWorkOrder_MissingTitleAndBuilding_Returns400FromDataAnnotations()
    {
        var response = await _client.PostAsJsonAsync("/api/workorders", new { title = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await ReadErrorKeys(response);
        Assert.Contains("Title", errors);
        Assert.Contains("BuildingId", errors);
    }

    [Fact]
    public async Task PostWorkOrder_TitleTooLong_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/workorders",
            new { title = new string('x', 151), buildingId = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Title", await ReadErrorKeys(response));
    }

    [Fact]
    public async Task PostWorkOrder_UnknownStatus_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/workorders",
            new { title = "Fix door", buildingId = 1, status = "Done" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostTechnician_InvalidEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/technicians",
            new { name = "Casey", email = "not-an-email", trade = "HVAC" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Email", await ReadErrorKeys(response));
    }

    [Fact]
    public async Task GetWorkOrders_StatusFilter_BindsFromQueryStringAndReturnsEnumNames()
    {
        var items = await _client.GetFromJsonAsync<List<JsonElement>>("/api/workorders?status=InProgress");

        var only = Assert.Single(items!);
        Assert.Equal("InProgress", only.GetProperty("status").GetString());
        Assert.Equal("High", only.GetProperty("priority").GetString());
    }

    [Fact]
    public async Task GetWorkOrder_Missing_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/workorders/999");

        // [Produces("application/json")] on the controllers sets the content type, so check the body shape.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("title", out _));
    }

    [Fact]
    public async Task DeleteTechnician_WithInProgressWork_Returns409()
    {
        var response = await _client.DeleteAsync("/api/technicians/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task<IReadOnlyCollection<string>> ReadErrorKeys(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("errors").EnumerateObject().Select(p => p.Name).ToList();
    }
}

/// <summary>An unexpected exception should come back as problem+json, not an empty 500 or a stack trace.</summary>
public class UnhandledExceptionTests(UnhandledExceptionTests.ThrowingApiFactory factory)
    : IClassFixture<UnhandledExceptionTests.ThrowingApiFactory>
{
    [Fact]
    public async Task UnhandledException_Returns500ProblemDetails()
    {
        var response = await factory.CreateClient().GetAsync("/api/workorders");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Simulated failure", body);
    }

    public class ThrowingApiFactory : ApiFactory
    {
        protected override void ConfigureExtraServices(IServiceCollection services)
        {
            services.RemoveAll<IWorkOrderService>();
            services.AddScoped<IWorkOrderService, ThrowingWorkOrderService>();
        }
    }

    private class ThrowingWorkOrderService : IWorkOrderService
    {
        private static InvalidOperationException Fail() => new("Simulated failure");
        public Task<IReadOnlyList<WorkOrderDto>> GetAllAsync(WorkOrderStatus? status, int? buildingId) => throw Fail();
        public Task<WorkOrderDto?> GetAsync(int id) => throw Fail();
        public Task<ServiceResult<WorkOrderDto>> CreateAsync(WorkOrderRequest request) => throw Fail();
        public Task<ServiceResult> UpdateAsync(int id, WorkOrderRequest request) => throw Fail();
        public Task<bool> DeleteAsync(int id) => throw Fail();
    }
}
