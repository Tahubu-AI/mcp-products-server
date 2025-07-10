namespace McpProductsServer.Options;

public record Chat
{
    public required string MaxContexWindow { get; init; }

    public required string CacheSimilarityScore { get; init; }

    public required string ProductMaxResults { get; init; }
}
