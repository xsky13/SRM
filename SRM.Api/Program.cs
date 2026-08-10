using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SRM.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DatabaseConnectionString")));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var hc = builder.Services.AddHealthChecks();
// check if app is running
hc.AddCheck(
    name: "self-live",
    check: () => HealthCheckResult.Healthy("Application is running"),
    tags: new[] { "live" }
);
// check if app can serve requests
hc.AddCheck(
    name: "self-ready",
    check: () => HealthCheckResult.Healthy("Application is ready to serve requests"),
    tags: new[] { "ready" }
);

var app = builder.Build();

// AGREGA LAS MIGRACIONES EN EL CONTAINER DOCKER
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    AllowCachingResponses = false,
    Predicate = r => r.Tags.Contains("live")
});

app.UseHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    AllowCachingResponses = false,
    Predicate = r => r.Tags.Contains("ready")
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
