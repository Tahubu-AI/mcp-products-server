// Program.cs
using Azure.Identity;
using McpProductsServer.Options;
using McpProductsServer.Services;
using McpProductsServer.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Debug: Print configuration sources and environment
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Content Root: {builder.Environment.ContentRootPath}");
Console.WriteLine($"Application Name: {builder.Environment.ApplicationName}");

// Ensure configuration sources are properly configured
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.RegisterConfiguration();
builder.Services.RegisterServices();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

// Debug: Log registered MCP tools
Console.WriteLine("Registering MCP tools...");
var toolTypes = typeof(Program).Assembly.GetTypes()
    .Where(t => t.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolTypeAttribute), false).Any())
    .ToList();
Console.WriteLine($"Found {toolTypes.Count} MCP tool types:");
foreach (var toolType in toolTypes)
{
    Console.WriteLine($"  - {toolType.Name}");
}

// OpenTelemetry is already configured by AddServiceDefaults()
// No need to configure it again

// Configure Azure Cosmos DB Aspire integration
var cosmosEndpoint = builder.Configuration.GetSection("CosmosDb:Endpoint").Value;
if (cosmosEndpoint is null)
{
    // Try alternative configuration paths
    cosmosEndpoint = builder.Configuration["CosmosDb:Endpoint"];
    if (cosmosEndpoint is null)
    {
        // Debug: Print available configuration sections
        var cosmosSection = builder.Configuration.GetSection("CosmosDb");
        var availableKeys = cosmosSection.GetChildren().Select(c => c.Key).ToList();
        throw new ArgumentException($"CosmosDb:Endpoint configuration value was not found. Available CosmosDb keys: {string.Join(", ", availableKeys)}. Please ensure it's configured in appsettings.Development.json.");
    }
}
builder.AddAzureCosmosClient(
    "cosmos-copilot",
    settings =>
    {
        settings.AccountEndpoint = new Uri(cosmosEndpoint);
        settings.Credential = new DefaultAzureCredential();
        settings.DisableTracing = false;
    },
    clientOptions => {
        clientOptions.ApplicationName = "cosmos-copilot";
        clientOptions.UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        clientOptions.CosmosClientTelemetryOptions = new()
        {
            CosmosThresholdOptions = new()
            {
                PointOperationLatencyThreshold = TimeSpan.FromMilliseconds(10),
                NonPointOperationLatencyThreshold = TimeSpan.FromMilliseconds(20)
            }
        };
    });

var app = builder.Build();

app.MapGet("/api/healthz", () => Results.Ok("Healthy"));

// Debug: Log MCP endpoint mapping
Console.WriteLine("Mapping MCP endpoints...");
app.MapMcp(); // Maps HTTP transport endpoints
Console.WriteLine("MCP HTTP endpoints mapped successfully.");

app.Run();

static class ProgramExtensions
{
    public static void RegisterConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<CosmosDb>()
            .Bind(builder.Configuration.GetSection(nameof(CosmosDb)));

        builder.Services.AddOptions<OpenAi>()
            .Bind(builder.Configuration.GetSection(nameof(OpenAi)));

        builder.Services.AddOptions<Chat>()
            .Bind(builder.Configuration.GetSection(nameof(Chat)));
    }

    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<CosmosDbService, CosmosDbService>();
        services.AddSingleton<ProductSearchTool, ProductSearchTool>();
        services.AddSingleton<ProductSummaryTool, ProductSummaryTool>();
        // services.AddSingleton<OpenAiService, OpenAiService>();
    }
}