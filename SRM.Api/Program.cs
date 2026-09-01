using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using SRM.Api;
using SRM.Api.Data;
using SRM.Api.Repositories;
using SRM.Api.Repositories.Interfaces;
using SRM.Api.Services;
using SRM.Api.Services.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Is(context.HostingEnvironment.IsDevelopment()
            ? Serilog.Events.LogEventLevel.Debug
            : Serilog.Events.LogEventLevel.Information)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            "/app/logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,           // solo guarda los ultimos 30 días. borra el resto
            fileSizeLimitBytes: 50_000_000,       // 50 MB por si un día explota de logs
            rollOnFileSizeLimit: true);           // si supera el límite de tamaño en el mismo día, arranca otro archivo
});

var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',')
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("DatabaseConnectionString"))
        .AddInterceptors(sp.GetRequiredService<SoftDeleteInterceptor>()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<SoftDeleteInterceptor>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddScoped<IApartmentService, ApartmentService>();

builder.Services.AddScoped<IApartmentRepository, ApartmentRepository>();

//agregar servicios de Repositorio.


builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc),
            new List<string>(Array.Empty<string>())
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["ApiSettings:Issuer"],
        ValidAudience = builder.Configuration["ApiSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("No jwt key present")))
    };
    options.Events.OnMessageReceived = context => {
        if (context.Request.Cookies.ContainsKey("X-Access-Token"))
            context.Token = context.Request.Cookies["X-Access-Token"];

        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

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

var imagesPath = Path.Combine(builder.Environment.ContentRootPath, "Storage", "Images");
Directory.CreateDirectory(imagesPath); // crear por las dudas

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagesPath),
    RequestPath = "/images"
});

var containerImagesPath = Path.Combine(builder.Environment.ContentRootPath, "Storage", "Images");
var seedImagesPath = Path.Combine(builder.Environment.ContentRootPath, "SeedData", "Images");

//if (app.Environment.IsDevelopment())
//{
    ImageSeeder.SeedImages(seedImagesPath, containerImagesPath); // just do it. nike
//}

app.UseCors();

 // logging, esconder requests a /health
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) =>
        httpContext.Request.Path.StartsWithSegments("/health")
            ? Serilog.Events.LogEventLevel.Verbose  // lo baja a un nivel que normalmente no se muestra/guarda
            : Serilog.Events.LogEventLevel.Information;
});

// AGREGA LAS MIGRACIONES EN EL CONTAINER DOCKER
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (app.Environment.IsDevelopment())
    {
        await DbSeeder.SeedAsync(db);
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
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

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
