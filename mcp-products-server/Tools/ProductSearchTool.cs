using System.ComponentModel;
using McpProductsServer.Models;
using McpProductsServer.Services;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace McpProductsServer.Tools;

[McpServerToolType]
public class ProductSearchTool
{
    private readonly CosmosDbService _cosmosDbService;
    private readonly ILogger<ProductSearchTool> _logger;

    public ProductSearchTool(CosmosDbService cosmosDbService, ILogger<ProductSearchTool> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    [McpServerTool(Name = "hybrid_search_products"),
        Description("Search for products using hybrid search (combines full-text and vector similarity). This method provides more relevant results by combining keyword matching with semantic understanding. Use this when you want to find products that are conceptually similar to your search terms, even if they don't contain the exact keywords. Best for: finding related products, understanding user intent, and getting more comprehensive search results.")]
    public async Task<List<Product>> HybridSearchProducts(
        [Description("The search text to find products")] string searchText,
        [Description("Vector embeddings for semantic search (1536-dimensional array)")] float[] vectors,
        [Description("Maximum number of products to return. Default value is 10")] int maxResults = 10
    )
    {
        _logger.LogInformation("🔍 MCP Tool Invoked: hybrid_search_products | SearchText: '{SearchText}' | VectorLength: {VectorLength} | MaxResults: {MaxResults}", 
            searchText, vectors?.Length ?? 0, maxResults);

        try
        {
            var startTime = DateTime.UtcNow;
            var results = await _cosmosDbService.HybridSearchProductsAsync(searchText, vectors, maxResults);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("✅ MCP Tool Completed: hybrid_search_products | Found {Count} products | Duration: {Duration}ms", 
                results.Count, duration.TotalMilliseconds);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MCP Tool Failed: hybrid_search_products | SearchText: '{SearchText}' | Error: {ErrorMessage}", 
                searchText, ex.Message);
            throw;
        }
    }

    [McpServerTool(Name = "get_product_by_id"),
        Description("Get a specific product by its ID")]
    public async Task<Product?> GetProductById(
        [Description("The unique identifier of the product")] string productId
    )
    {
        _logger.LogInformation("🔍 MCP Tool Invoked: get_product_by_id | ProductId: '{ProductId}'", productId);

        try
        {
            var startTime = DateTime.UtcNow;
            var result = await _cosmosDbService.GetProductByIdAsync(productId);
            var duration = DateTime.UtcNow - startTime;

            if (result != null)
            {
                _logger.LogInformation("✅ MCP Tool Completed: get_product_by_id | ProductId: '{ProductId}' | Found: Yes | Duration: {Duration}ms", 
                    productId, duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogWarning("⚠️ MCP Tool Completed: get_product_by_id | ProductId: '{ProductId}' | Found: No | Duration: {Duration}ms", 
                    productId, duration.TotalMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MCP Tool Failed: get_product_by_id | ProductId: '{ProductId}' | Error: {ErrorMessage}", 
                productId, ex.Message);
            throw;
        }
    }
} 