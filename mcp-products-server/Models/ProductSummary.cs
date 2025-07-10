namespace McpProductsServer.Models;

public class ProductSummary
{
    public string id { get; set; } = string.Empty;
    public string productId { get; set; } = string.Empty;
    public string title { get; set; } = string.Empty;
    public string shortDescription { get; set; } = string.Empty;
    public string marketingCopy { get; set; } = string.Empty;
    public List<string> keyFeatures { get; set; } = new();
    public List<string> targetAudience { get; set; } = new();
    public string useCase { get; set; } = string.Empty;
    public DateTime createdAt { get; set; } = DateTime.UtcNow;
    public DateTime updatedAt { get; set; } = DateTime.UtcNow;
}