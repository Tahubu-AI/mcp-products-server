using System.ComponentModel;
using McpProductsServer.Models;
using McpProductsServer.Services;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace McpProductsServer.Tools;

[McpServerToolType]
public class ProductSummaryTool
{
    private readonly CosmosDbService _cosmosDbService;
    private readonly ILogger<ProductSummaryTool> _logger;

    public ProductSummaryTool(CosmosDbService cosmosDbService, ILogger<ProductSummaryTool> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    [McpServerTool(Name = "generate_product_summary"),
        Description("Generate and save a marketing summary for a product. The LLM should provide the creative marketing content (title, short description, marketing copy) based on the product data. The tool will automatically generate the use case, key features, and target audience based on the product information, then save the complete summary to the database.")]
    public async Task<ProductSummary> GenerateProductSummary(
        [Description("The product ID to generate a summary for")] string productId,
        [Description("A catchy, marketing-focused title for the product (e.g., 'Revolutionary Mountain Bike for Adventure Seekers')")] string title,
        [Description("A brief, compelling description of the product (2-3 sentences max)")] string shortDescription,
        [Description("Engaging marketing copy that highlights the product's benefits and appeals to potential customers")] string marketingCopy
    )
    {
        _logger.LogInformation("🔍 MCP Tool Invoked: generate_product_summary | ProductId: '{ProductId}' | Title: '{Title}'", 
            productId, title);

        try
        {
            var startTime = DateTime.UtcNow;
            
            // Get the product data to generate automated fields
            var product = await _cosmosDbService.GetProductByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("⚠️ MCP Tool Failed: generate_product_summary | ProductId: '{ProductId}' | Error: Product not found", productId);
                throw new ArgumentException($"Product with ID '{productId}' not found.");
            }

            // Generate a unique ID for the summary
            var summaryId = Guid.NewGuid().ToString();
            
            // Create the comprehensive marketing summary
            var summary = new ProductSummary
            {
                id = summaryId,
                productId = productId,
                title = title,
                shortDescription = shortDescription,
                marketingCopy = marketingCopy,
                keyFeatures = ExtractKeyFeatures(product),
                targetAudience = DetermineTargetAudience(product),
                useCase = GenerateUseCase(product),
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow
            };

            // Save the summary to Cosmos DB
            await _cosmosDbService.SaveProductSummaryAsync(summary);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("✅ MCP Tool Completed: generate_product_summary | ProductId: '{ProductId}' | SummaryId: '{SummaryId}' | Duration: {Duration}ms", 
                productId, summaryId, duration.TotalMilliseconds);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MCP Tool Failed: generate_product_summary | ProductId: '{ProductId}' | Error: {ErrorMessage}", 
                productId, ex.Message);
            throw;
        }
    }

    [McpServerTool(Name = "get_product_summary"),
        Description("Retrieve a previously generated product summary by product ID")]
    public async Task<ProductSummary?> GetProductSummary(
        [Description("The product ID to get the summary for")] string productId
    )
    {
        _logger.LogInformation("🔍 MCP Tool Invoked: get_product_summary | ProductId: '{ProductId}'", productId);

        try
        {
            var startTime = DateTime.UtcNow;
            var result = await _cosmosDbService.GetProductSummaryAsync(productId);
            var duration = DateTime.UtcNow - startTime;

            if (result != null)
            {
                _logger.LogInformation("✅ MCP Tool Completed: get_product_summary | ProductId: '{ProductId}' | Found: Yes | Duration: {Duration}ms", 
                    productId, duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogWarning("⚠️ MCP Tool Completed: get_product_summary | ProductId: '{ProductId}' | Found: No | Duration: {Duration}ms", 
                    productId, duration.TotalMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MCP Tool Failed: get_product_summary | ProductId: '{ProductId}' | Error: {ErrorMessage}", 
                productId, ex.Message);
            throw;
        }
    }

    [McpServerTool(Name = "update_product_summary"),
        Description("Update an existing product summary with new marketing content. The LLM should provide updated creative content while the tool handles automated fields.")]
    public async Task<ProductSummary> UpdateProductSummary(
        [Description("The product ID to update the summary for")] string productId,
        [Description("Updated catchy, marketing-focused title for the product")] string title,
        [Description("Updated brief, compelling description of the product")] string shortDescription,
        [Description("Updated engaging marketing copy")] string marketingCopy
    )
    {
        _logger.LogInformation("🔍 MCP Tool Invoked: update_product_summary | ProductId: '{ProductId}' | Title: '{Title}'", 
            productId, title);

        try
        {
            var startTime = DateTime.UtcNow;
            
            // Get the existing summary
            var existingSummary = await _cosmosDbService.GetProductSummaryAsync(productId);
            if (existingSummary == null)
            {
                _logger.LogWarning("⚠️ MCP Tool Failed: update_product_summary | ProductId: '{ProductId}' | Error: Product summary not found", productId);
                throw new ArgumentException($"Product summary for ID '{productId}' not found.");
            }

            // Get the product data to regenerate automated fields
            var product = await _cosmosDbService.GetProductByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("⚠️ MCP Tool Failed: update_product_summary | ProductId: '{ProductId}' | Error: Product not found", productId);
                throw new ArgumentException($"Product with ID '{productId}' not found.");
            }

            // Update the summary with new content
            existingSummary.title = title;
            existingSummary.shortDescription = shortDescription;
            existingSummary.marketingCopy = marketingCopy;
            existingSummary.keyFeatures = ExtractKeyFeatures(product);
            existingSummary.targetAudience = DetermineTargetAudience(product);
            existingSummary.useCase = GenerateUseCase(product);
            existingSummary.updatedAt = DateTime.UtcNow;

            // Save the updated summary
            await _cosmosDbService.SaveProductSummaryAsync(existingSummary);
            var duration = DateTime.UtcNow - startTime;
            
            _logger.LogInformation("✅ MCP Tool Completed: update_product_summary | ProductId: '{ProductId}' | Duration: {Duration}ms", 
                productId, duration.TotalMilliseconds);
            
            return existingSummary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MCP Tool Failed: update_product_summary | ProductId: '{ProductId}' | Error: {ErrorMessage}", 
                productId, ex.Message);
            throw;
        }
    }

    [McpServerTool(Name = "delete_product_summary"),
        Description("Delete a product summary")]
    public async Task DeleteProductSummary(
        [Description("The product ID to delete the summary for")] string productId
    )
    {
        _logger.LogInformation("🔍 MCP Tool Invoked: delete_product_summary | ProductId: '{ProductId}'", productId);

        try
        {
            var startTime = DateTime.UtcNow;
            await _cosmosDbService.DeleteProductSummaryAsync(productId);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("✅ MCP Tool Completed: delete_product_summary | ProductId: '{ProductId}' | Duration: {Duration}ms", 
                productId, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MCP Tool Failed: delete_product_summary | ProductId: '{ProductId}' | Error: {ErrorMessage}", 
                productId, ex.Message);
            throw;
        }
    }

    [McpServerTool(Name = "list_product_summaries"),
        Description("Get all product summaries in the database")]
    public async Task<List<ProductSummary>> ListProductSummaries()
    {
        _logger.LogInformation("🔍 MCP Tool Invoked: list_product_summaries");

        try
        {
            var startTime = DateTime.UtcNow;
            var results = await _cosmosDbService.GetAllProductSummariesAsync();
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("✅ MCP Tool Completed: list_product_summaries | Found {Count} summaries | Duration: {Duration}ms", 
                results.Count, duration.TotalMilliseconds);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MCP Tool Failed: list_product_summaries | Error: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    // Helper methods for generating automated content
    private List<string> ExtractKeyFeatures(Product product)
    {
        var features = new List<string>();
        
        // Extract features from tags
        features.AddRange(product.tags.Take(3));
        
        // Add price-based feature
        if (product.price > 1000)
            features.Add("Premium Quality");
        else if (product.price > 500)
            features.Add("Professional Grade");
        else
            features.Add("Great Value");
            
        // Add category-based feature
        features.Add($"{product.categoryName} Specialized");
        
        return features.Distinct().ToList();
    }

    private List<string> DetermineTargetAudience(Product product)
    {
        var audiences = new List<string>();
        
        // Determine audience based on price
        if (product.price > 1000)
            audiences.Add("Professional Users");
        else if (product.price > 500)
            audiences.Add("Enthusiasts");
        else
            audiences.Add("Beginners");
            
        // Determine audience based on category
        if (product.categoryName.Contains("Mountain", StringComparison.OrdinalIgnoreCase))
            audiences.Add("Adventure Seekers");
        else if (product.categoryName.Contains("Road", StringComparison.OrdinalIgnoreCase))
            audiences.Add("Speed Enthusiasts");
        else if (product.categoryName.Contains("City", StringComparison.OrdinalIgnoreCase))
            audiences.Add("Urban Commuters");
            
        return audiences.Distinct().ToList();
    }

    private string GenerateUseCase(Product product)
    {
        if (product.categoryName.Contains("Mountain", StringComparison.OrdinalIgnoreCase))
            return "Perfect for off-road adventures, trail riding, and mountain biking excursions.";
        else if (product.categoryName.Contains("Road", StringComparison.OrdinalIgnoreCase))
            return "Ideal for road cycling, racing, and long-distance rides on paved surfaces.";
        else if (product.categoryName.Contains("City", StringComparison.OrdinalIgnoreCase))
            return "Great for daily commuting, urban exploration, and casual city riding.";
        else
            return "Versatile design suitable for various cycling activities and terrains.";
    }
} 