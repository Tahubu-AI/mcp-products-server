using Azure;
using Microsoft.Extensions.VectorData;

namespace McpProductsServer.Models
{
    public class Product
    {
        [VectorStoreKey]
        public string id { get; set; }
        [VectorStoreData]
        public string categoryId { get; set; }
        [VectorStoreData]
        public string categoryName { get; set; }
        [VectorStoreData]
        public string sku { get; set; }
        [VectorStoreData]
        public string name { get; set; }
        [VectorStoreData]
        public string description { get; set; }
        [VectorStoreData]
        public double price { get; set; }
        [VectorStoreData]
        public List<string> tags { get; set; }
        //[VectorStoreRecordData]
        //public List<Review> reviews { get; set; }

        [VectorStoreVector(Dimensions: 1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.DiskAnn)]
        public ReadOnlyMemory<float>? vectors { get; set; }

        public Product(string id, string categoryId, string categoryName, string sku, string name, string description, double price, List<string> tags, ReadOnlyMemory<float>? vectors = null)
        {
            this.id = id;
            this.categoryId = categoryId;
            this.categoryName = categoryName;
            this.sku = sku;
            this.name = name;
            this.description = description;
            this.price = price;
            this.tags = tags;
            //this.reviews = reviews;
            this.vectors = vectors;
        }

        public override string ToString()
        {
            return $"id: {id}, categoryId: {categoryId}, categoryName: {categoryName}, sku: {sku}, name: {name}, description: {description}, price: {price}, tags: {string.Join(", ", tags)}";
        }

    }
    public class Tag
    {
        public string id { get; set; }
        public string name { get; set; }

        public Tag(string id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }

    public class Review
    {
        public string customer { get; set; }
        public int rating { get; set; }
        public string review { get; set; }

        public Review(string customer, int rating, string review)
        {
            this.customer = customer;
            this.rating = rating;
            this.review = review;
        }
    }
}
