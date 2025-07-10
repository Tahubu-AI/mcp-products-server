using McpProductsServer.Models;
using McpProductsServer.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Container = Microsoft.Azure.Cosmos.Container;
using PartitionKey = Microsoft.Azure.Cosmos.PartitionKey;

namespace McpProductsServer.Services
{
    /// <summary>
    /// Service to access Azure Cosmos DB for NoSQL - Products and Product Summaries.
    /// </summary>
    public class CosmosDbService
    {
        private readonly Container _productContainer;
        private readonly Container _productSummaryContainer;
        private readonly string _productDataSourceURI;

        /// <summary>
        /// Creates a new instance of the service.
        /// </summary>
        /// <param name="client">CosmosClient injected via DI.</param>
        /// <param name="cosmosOptions">Options.</param>
        /// <exception cref="ArgumentNullException">Thrown when endpoint, key, databaseName, or productContainerName is either null or empty.</exception>
        /// <remarks>
        /// This constructor will validate credentials and create a service client instance.
        /// </remarks>
        public CosmosDbService(CosmosClient client, IOptions<CosmosDb> cosmosOptions)
        {
            var databaseName = cosmosOptions.Value.Database;
            var productContainerName = cosmosOptions.Value.ProductContainer;
            var productSummaryContainerName = "product-summaries";
            var productDataSourceURI = cosmosOptions.Value.ProductDataSourceURI;

            ArgumentNullException.ThrowIfNullOrEmpty(databaseName);
            ArgumentNullException.ThrowIfNullOrEmpty(productContainerName);
            ArgumentNullException.ThrowIfNullOrEmpty(productDataSourceURI);

            _productDataSourceURI = productDataSourceURI;

            Database database = client.GetDatabase(databaseName)!;
            Container productContainer = database.GetContainer(productContainerName)!;
            Container productSummaryContainer = database.GetContainer(productSummaryContainerName)!;

            _productContainer =
                productContainer
                ?? throw new ArgumentException(
                    "Unable to connect to existing Azure Cosmos DB container or database."
                );

            _productSummaryContainer =
                productSummaryContainer
                ?? throw new ArgumentException(
                    "Unable to connect to existing Azure Cosmos DB container or database."
                );
        }

        /// <summary>
        /// Performs full text search on the CosmosDB product container
        /// </summary>
        /// <param name="promptText">Text used to do the search</param>
        /// <param name="productMaxResults">Limit the number of returned items</param>
        /// <returns>List of returned products</returns>
        public async Task<List<Product>> FullTextSearchProductsAsync(string promptText, int productMaxResults)
        {
            List<Product> results = new();

            string[] words =
                promptText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string rankedWords = $"[{string.Join(", ", words.Select(word => $"'{word}'"))}]";

            string queryText = $"""
                                    SELECT
                                        Top {productMaxResults} c.id, c.categoryId, c.categoryName, c.sku, c.name, c.description, c.price, c.tags
                                    FROM c
                                    WHERE
                                        FullTextContainsAny(c.description, {rankedWords}) OR
                                        FullTextContainsAny(c.tags, {rankedWords})
                                """;

            var queryDef = new QueryDefinition(query: queryText);

            using FeedIterator<Product> resultSet = _productContainer.GetItemQueryIterator<Product>(
                queryDefinition: queryDef
            );

            while (resultSet.HasMoreResults)
            {
                FeedResponse<Product> response = await resultSet.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        /// <summary>
        /// Performs hybrid search on the CosmosDB product container
        /// </summary>
        /// <param name="promptText">Text used to do the search</param>
        /// <param name="promptVectors">Vectors used to do the search</param>
        /// <param name="productMaxResults">Limit the number of returned items</param>
        /// <returns>List of returned products</returns>
        public async Task<List<Product>> HybridSearchProductsAsync(
            string promptText,
            float[] promptVectors,
            int productMaxResults
        )
        {
            List<Product> results = new();

            string[] words =
                promptText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string rankedWords = $"[{string.Join(", ", words.Select(word => $"'{word}'"))}]";

            string queryText = $"""
                                    SELECT
                                        Top {productMaxResults} c.id, c.categoryId, c.categoryName, c.sku, c.name, c.description, c.price, c.tags
                                    FROM c
                                    ORDER BY RANK RRF(
                                        FullTextScore(c.description, {rankedWords}),
                                        FullTextScore(c.tags, {rankedWords}),
                                        VectorDistance(c.vectors, @vectors)
                                        )
                                """;

            var queryDef = new QueryDefinition(query: queryText)
                .WithParameter("@vectors", promptVectors);

            using FeedIterator<Product> resultSet = _productContainer.GetItemQueryIterator<Product>(
                queryDefinition: queryDef
            );

            while (resultSet.HasMoreResults)
            {
                FeedResponse<Product> response = await resultSet.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        /// <summary>
        /// Get a product by its ID
        /// </summary>
        /// <param name="productId">Product ID to retrieve</param>
        /// <returns>Product if found, null otherwise</returns>
        public async Task<Product?> GetProductByIdAsync(string productId)
        {
            try
            {
                var queryText = "SELECT * FROM c WHERE c.id = @productId";
                var queryDef = new QueryDefinition(queryText).WithParameter("@productId", productId);

                using FeedIterator<Product> resultSet = _productContainer.GetItemQueryIterator<Product>(queryDef);

                while (resultSet.HasMoreResults)
                {
                    FeedResponse<Product> response = await resultSet.ReadNextAsync();
                    return response.FirstOrDefault();
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Get all products
        /// </summary>
        /// <returns>List of all products</returns>
        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();
            var queryText = "SELECT * FROM c";

            using FeedIterator<Product> resultSet = _productContainer.GetItemQueryIterator<Product>(queryText);

            while (resultSet.HasMoreResults)
            {
                FeedResponse<Product> response = await resultSet.ReadNextAsync();
                products.AddRange(response);
            }

            return products;
        }

        /// <summary>
        /// Save a product summary to the database
        /// </summary>
        /// <param name="summary">Product summary to save</param>
        public async Task SaveProductSummaryAsync(ProductSummary summary)
        {
            await _productSummaryContainer.UpsertItemAsync(summary, new PartitionKey(summary.productId));
        }

        /// <summary>
        /// Get a product summary by product ID
        /// </summary>
        /// <param name="productId">Product ID to get summary for</param>
        /// <returns>Product summary if found, null otherwise</returns>
        public async Task<ProductSummary?> GetProductSummaryAsync(string productId)
        {
            try
            {
                var queryText = "SELECT * FROM c WHERE c.productId = @productId";
                var queryDef = new QueryDefinition(queryText).WithParameter("@productId", productId);

                using FeedIterator<ProductSummary> resultSet = _productSummaryContainer.GetItemQueryIterator<ProductSummary>(queryDef);

                while (resultSet.HasMoreResults)
                {
                    FeedResponse<ProductSummary> response = await resultSet.ReadNextAsync();
                    return response.FirstOrDefault();
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Delete a product summary by product ID
        /// </summary>
        /// <param name="productId">Product ID to delete summary for</param>
        public async Task DeleteProductSummaryAsync(string productId)
        {
            try
            {
                var summary = await GetProductSummaryAsync(productId);
                if (summary != null)
                {
                    await _productSummaryContainer.DeleteItemAsync<ProductSummary>(
                        id: summary.id,
                        partitionKey: new PartitionKey(productId)
                    );
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Summary doesn't exist, nothing to delete
            }
        }

        /// <summary>
        /// Get all product summaries
        /// </summary>
        /// <returns>List of all product summaries</returns>
        public async Task<List<ProductSummary>> GetAllProductSummariesAsync()
        {
            var summaries = new List<ProductSummary>();
            var queryText = "SELECT * FROM c";

            using FeedIterator<ProductSummary> resultSet = _productSummaryContainer.GetItemQueryIterator<ProductSummary>(queryText);

            while (resultSet.HasMoreResults)
            {
                FeedResponse<ProductSummary> response = await resultSet.ReadNextAsync();
                summaries.AddRange(response);
            }

            return summaries;
        }
    }
}

