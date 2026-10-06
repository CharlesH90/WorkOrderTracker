using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WorkOrderTracker.Api.Data;
using WorkOrderTracker.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// The connection string holds a password, so it never lives in appsettings.json.
// Locally it comes from user-secrets (see README); elsewhere from the
// ConnectionStrings__DefaultConnection environment variable.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not set. Run: dotnet user-secrets set " +
            "\"ConnectionStrings:DefaultConnection\" \"<connection string>\" (see README).")));

builder.Services.Configure<BusinessOptions>(builder.Configuration.GetSection("Business"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IWorkOrderService, WorkOrderService>();

// Turns unhandled exceptions into application/problem+json instead of an empty 500.
builder.Services.AddProblemDetails();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Work Order Tracker API", Version = "v1" });
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Apply pending migrations on startup so a fresh Docker database is ready to go.
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseCors();
app.MapControllers();

app.Run();

// Exposes the entry point so integration tests can host the API with WebApplicationFactory<Program>.
public partial class Program { }
