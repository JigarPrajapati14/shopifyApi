using Microsoft.Azure.Cosmos;
using ShopifyMvc.ApplicationDbContext;

namespace ShopifyMvc.Service
{
    public class ShopifyService
    {
        private readonly IConfiguration _configuration;
        private readonly DataContext _context;
        private readonly Container _cosmosContainer;

        public ShopifyService(DataContext context, IConfiguration configuration)
        {
            _configuration = configuration;
            _context = context;

            try
            {
                var cosmosEndpoint = _configuration["CosmosDb:Endpoint"];
                var cosmosKey = _configuration["CosmosDb:Key"];
                var databaseName = _configuration["CosmosDb:DatabaseName"];
                var containerName = _configuration["CosmosDb:ContainerName"];

                var cosmosClientOptions = new CosmosClientOptions
                {
                    AllowBulkExecution = true
                };

                var cosmosClient = new CosmosClient(cosmosEndpoint, cosmosKey, cosmosClientOptions);
                var database = cosmosClient.GetDatabase(databaseName);
                _cosmosContainer = database.GetContainer(containerName);
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"Cosmos DB Error: {ex.StatusCode}, RequestCharge: {ex.RequestCharge}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Diagnostics: {ex.Diagnostics}");
                throw;
            }
        }
        public async Task<string> DeleteDataFromCosmosDBAsync(string productDelete)
        {
            try
            {
                var cosmosEndpoint = _configuration["CosmosDb:Endpoint"];
                var cosmosKey = _configuration["CosmosDb:Key"];
                var databaseName = "product-db";
                var containerName = "ShopifyProduct";

                var cosmosClient = new CosmosClient(cosmosEndpoint, cosmosKey);
                var database = cosmosClient.GetDatabase(databaseName);
                var container = database.GetContainer(containerName);
                string partitionKeyToDelete = productDelete;

                var queryDefinition = new QueryDefinition($"SELECT * FROM c WHERE c.ProductId = @ProductId")
           .WithParameter("@ProductId", partitionKeyToDelete);

                var iterator = container.GetItemQueryIterator<dynamic>(queryDefinition);

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();

                    if (response.Count > 0)
                    {
                        foreach (var item in response)
                        {
                            var result = await container.DeleteItemAsync<object>(item.id.ToString(), new PartitionKey(partitionKeyToDelete));
                        }
                        return $"Deleted {response.Count} documents with SKU: {productDelete}";
                    }
                }
                return $"No documents found with SKU: {productDelete}";
            }
            catch (Exception ex)
            {
                return $"Error deleting documents: {ex.Message}";
            }
        }
    }
}
