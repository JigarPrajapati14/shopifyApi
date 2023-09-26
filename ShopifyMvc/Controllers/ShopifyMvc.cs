using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ShopifyMvc.ApplicationDbContext;
using ShopifyMvc.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Cosmos;
using ShopifyMvc.Service;
using Microsoft.CodeAnalysis;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Text;

namespace ShopifyMvc.Controllers
{
    public class ShopifyMvcController : Controller // Make sure the controller name matches the class name
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ShopifyMvcController> _logger; // Correct the ILogger type
        private readonly ShopifyService _shopifyService;
        private readonly IConfiguration _configuration;

        public ShopifyMvcController(IHttpClientFactory httpClientFactory, ILogger<ShopifyMvcController> logger, ShopifyService shopifyService, IConfiguration configuration) // Correct the parameter names
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _shopifyService = shopifyService;
            _configuration = configuration;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            using (var dataContext = new DataContext())
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    var apiUrl = "https://localhost:7103/api/Shopify";
                    var httpResponseMessage = await client.GetAsync(apiUrl);
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        var jsonResponse = await httpResponseMessage.Content.ReadAsStringAsync();
                        var responseList = JsonConvert.DeserializeObject<List<shopify>>(jsonResponse);
                        var productsToDisplay = new List<shopify>();

                        foreach (var product in responseList)
                        {
                            // Check if a similar product already exists in Cosmos DB
                            var existingProduct = await dataContext.ShopifyProduct.FirstOrDefaultAsync(p => p.ProductId == product.ProductId);

                            if (existingProduct == null)
                            {
                                // If not, add it as a new document
                                product.id = Guid.NewGuid().ToString();
                                dataContext.ShopifyProduct.Add(product);
                            }
                            else
                            {
                                // If it exists, add it to the list of products to display
                                productsToDisplay.Add(existingProduct);
                            }
                        }

                        await dataContext.SaveChangesAsync();

                        // Display the products, which may include both new and existing data
                        return View(productsToDisplay);
                    }
                    else
                    {
                        return View("Error");
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "JSON Exception occurred.");
                    return View("Error");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An exception occurred.");
                    return View("Error");
                }
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                string endpointUrl = _configuration["CosmosDb:Endpoint"];
                string primaryKey = _configuration["CosmosDb:Key"];
                string databaseName = _configuration["CosmosDb:DatabaseName"];
                string containerName = _configuration["CosmosDb:ContainerName"];
                // Create CosmosClient
                var cosmosClient = new CosmosClient(endpointUrl, primaryKey);
                // Get a reference to the database and container
                var database = cosmosClient.GetDatabase(databaseName);
                var container = database.GetContainer(containerName);
                // Retrieve the document by ID
                var queryText = $"SELECT * FROM c WHERE c.id = '{id}'";
                var queryDefinition = new QueryDefinition(queryText);
                var queryResultSetIterator = container.GetItemQueryIterator<Product>(queryDefinition);
                var documents = new List<Product>();
                while (queryResultSetIterator.HasMoreResults)
                {
                    var response = await queryResultSetIterator.ReadNextAsync();
                    documents.AddRange(response);
                }
                ViewBag.ProductId = id;
                return View(documents);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred: {ex.Message}";
                return View("Error");
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateProducts(List<Product> products)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string endpointUrl = _configuration["CosmosDb:Endpoint"];
                    string primaryKey = _configuration["CosmosDb:Key"];
                    string databaseName = _configuration["CosmosDb:DatabaseName"];
                    string containerName = _configuration["CosmosDb:ContainerName"];
                    var cosmosClient = new CosmosClient(endpointUrl, primaryKey);
                    var database = cosmosClient.GetDatabase(databaseName);
                    var container = database.GetContainer(containerName);
                    foreach (var item in products)
                    {
                        var cosmosDbData = MapToCosmosDbData(item);
                        var response = await container.UpsertItemAsync(cosmosDbData);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var jObject = (JObject)response.Resource;
                            var product = new Product
                            {
                                Id = jObject["Id"]?.ToString()
                            };
                        }
                        else
                        {
                            Console.WriteLine($"Cosmos DB Update Error: {response.StatusCode}, RequestCharge: {response.RequestCharge}");
                            Console.WriteLine($"Diagnostics: {response.Diagnostics}");
                            throw new Exception($"Cosmos DB Update Error: {response.StatusCode}");
                        }
                    }
                }
                catch (CosmosException ex)
                {
                    Console.WriteLine($"Cosmos DB Error: {ex.StatusCode}, RequestCharge: {ex.RequestCharge}");
                    Console.WriteLine($"Message: {ex.Message}");
                    Console.WriteLine($"Diagnostics: {ex.Diagnostics}");
                    throw;
                }
            }
            return RedirectToAction("Index"); // You can customize this as needed
        }
        private object MapToCosmosDbData(Product updatedProduct)
        {
            var cosmosDbData = new
            {
                id = updatedProduct.Id,
                Discriminator = "shopify",
                ProductId = updatedProduct.ProductId,
                title = updatedProduct.title,
                price = updatedProduct.price,
                variantId = updatedProduct.variantId,
            };
            return cosmosDbData;
        }
        [HttpPost]
        public async Task<IActionResult> updateShopify(List<Product> updatedProducts)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var apiUrl = "https://localhost:7103/api/Shopify/updateShopifyProduct";
                foreach (var updatedProduct in updatedProducts)
                {
                    var updateRequest = new UpdateShopifyProductRequest
                    {
                        id = updatedProduct.ProductId,
                        title = updatedProduct.title,
                        variants = new List<Variant>
                {
                    new Variant
                    {
                        id = updatedProduct.variantId,
                        price = updatedProduct.price
                    }
                }
                    };
                    var jsonContent = JsonConvert.SerializeObject(updateRequest);
                    var httpResponseMessage = await client.PostAsync(apiUrl, new StringContent(jsonContent, Encoding.UTF8, "application/json"));
                    if (!httpResponseMessage.IsSuccessStatusCode)
                    {
                        return View("Error");
                    }
                    else
                    {
                        var productDelete = updatedProduct.ProductId;
                        var cosmosDeleteResult = await _shopifyService.DeleteDataFromCosmosDBAsync(productDelete);
                        if (!string.IsNullOrEmpty(cosmosDeleteResult) && cosmosDeleteResult.Contains("Deleted"))
                        {
                            // Data was successfully deleted
                            TempData["Message"] = cosmosDeleteResult;
                        }
                        else
                        {
                            // Handle the case where deletion from Cosmos DB fails
                            return View("Error");
                        }
                    }
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return View("Error");
            }
        }
    }
}



