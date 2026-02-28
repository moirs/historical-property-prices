using Polly;
using Polly.Extensions.Http;
using PropertyPrices.Core;
using PropertyPrices.Infrastructure.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, loggerConfig) =>
{
    var logLevel = context.Configuration.GetValue<string>("Logging:LogLevel:Default") ?? "Information";

    loggerConfig
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "PropertyPrices.Api")
        .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

    if (builder.Environment.IsDevelopment())
    {
        loggerConfig.MinimumLevel.Debug();
    }
});

// Add services
builder.Services.AddOpenApi();

// Configure SPARQL options
var sparqlOptions = new SparqlEndpointOptions();
builder.Configuration.GetSection("SparqlEndpoint").Bind(sparqlOptions);
builder.Services.AddSingleton(sparqlOptions);

// Configure HTTP client for SPARQL queries with Polly resilience policies
var sparqlHttpPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .Or<HttpRequestException>()
    .OrResult(r => (int)r.StatusCode == 503) // Service Unavailable
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt =>
            TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff: 2s, 4s, 8s

builder.Services
    .AddHttpClient<SparqlDataAccessClient>(client =>
    {
        client.BaseAddress = new Uri(sparqlOptions.Url);
        client.Timeout = TimeSpan.FromSeconds(sparqlOptions.TimeoutSeconds);
    });

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .Produces<HealthResponse>(StatusCodes.Status200OK)
    .WithDescription("Health check endpoint");

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("PropertyPrices API starting...");

app.Run();

record HealthResponse(string Status, DateTime Timestamp);

