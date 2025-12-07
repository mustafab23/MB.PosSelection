using HealthChecks.UI.Client;
using MB.PosSelection.Api.Middlewares;
using MB.PosSelection.Application;
using MB.PosSelection.Infrastructure;
using MB.PosSelection.Infrastructure.Persistence;
using MB.PosSelection.Infrastructure.Services.Metrics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var serviceName = "MB.PosSelection.Api";
var serviceVersion = "1.0.0";

// -------------------------------------------------------------------------
// 1. SERVICES (DI Container Setup)
// -------------------------------------------------------------------------

// OpenTelemetry Configuration
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation()
        // .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(PosMetricsService.MeterName)
        .AddOtlpExporter((exporterOptions, readerOptions) =>
        {
            // Prometheus/Grafana için export aralığı
            readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
        }));

// Layer Dependencies
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// API Controllers & Routing
builder.Services.AddControllers();
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Smart POS Selection API",
        Version = "v1",
        Description = "E-ticaret işlemleri için en düşük maliyetli sanal POS'u (VPOS) seçen, yüksek performanslı ve akıllı karar motoru API'si."
    });

    var apiXmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var apiXmlPath = Path.Combine(AppContext.BaseDirectory, apiXmlFile);
    options.IncludeXmlComments(apiXmlPath);

    var appXmlFile = $"{typeof(MB.PosSelection.Application.DependencyInjection).Assembly.GetName().Name}.xml";
    var appXmlPath = Path.Combine(AppContext.BaseDirectory, appXmlFile);
    options.IncludeXmlComments(appXmlPath);
});

// Exception Handling & ProblemDetails
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Health Checks (Postgres & Redis)
var dbConnString = builder.Configuration.GetConnectionString("DefaultConnection");
var redisConnString = builder.Configuration.GetConnectionString("Redis");

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddNpgSql(dbConnString!, name: "PostgreSQL", tags: new[] { "ready", "db" })
    .AddRedis(redisConnString!, name: "Redis", tags: new[] { "ready", "cache" });

// Rate Limiting Configuration
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 60; // Dakikada 60 istek
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// -------------------------------------------------------------------------
// 2. INITIALIZATION (Seed & Migration)
// -------------------------------------------------------------------------

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Veritabanı yoksa oluştur, migrationları uygula
        await context.Database.MigrateAsync();
        // Test verilerini bas
        await AppDbContextSeed.SeedAsync(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Veritabanı hazırlanırken hata: {ex.Message}");
    }
}

// -------------------------------------------------------------------------
// 3. MIDDLEWARE PIPELINE
// -------------------------------------------------------------------------

// Development Environment Tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

// HTTPS Redirection
app.UseHttpsRedirection();

// Routing (İsteğin hangi endpoint'e gideceğini belirler)
app.UseRouting();

// Rate Limiter
app.UseRateLimiter();

app.UseAuthorization();

// Health Check Endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Controllers Map
// Tüm controller'lara varsayılan rate limit politikasını uygula
app.MapControllers()
   .RequireRateLimiting("fixed");

app.Run();

// Integration Test erişimi için partial class tanımı
public partial class Program { }