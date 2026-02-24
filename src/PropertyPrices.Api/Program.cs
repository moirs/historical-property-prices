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
