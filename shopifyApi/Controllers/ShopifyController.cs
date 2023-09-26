using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace shopifyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopifyController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetShopifyProducts()
        {
            try
            {
                string apiKey = "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";
                string storeName = "quickstart-e8be2cd1";
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", apiKey);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://{storeName}.myshopify.com/admin/api/2023-07/products.json");
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var products = JObject.Parse(jsonResponse)["products"];
                    var productList = new List<ShopifyProductViewModel>();
                    foreach (var product in products)
                    {
                        string id = product["id"].ToString();
                        string title = product["title"].ToString();
                        var variants = product["variants"];
                        foreach (var variant in variants)
                        {
                            string variantId = variant["id"].ToString();
                            string price = variant["price"].ToString();
                            var viewModel = new ShopifyProductViewModel
                            {
                                ProductId = id,
                                Title = title,
                                VariantId = variantId,
                                Price = price
                            };

                            productList.Add(viewModel);
                        }
                    }
                    return Ok(productList);
                }
                else
                {
                    return BadRequest($"Request failed with status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpPost]
        [Route("updateShopifyProduct")]
        public async Task<IActionResult> UpdateShopifyProduct(Product updateProduct)
        {
            try
            {
                string apiKey = "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";
                string storeName = "quickstart-e8be2cd1";

                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", apiKey);

                var apiUrl = $"https://{storeName}.myshopify.com/admin/api/2023-07/products/{updateProduct.id}.json"; // Use updateProduct.Id
                var jsonContent = JsonConvert.SerializeObject(new { product = updateProduct });
                var updateContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(apiUrl, updateContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return Content(responseContent, "application/json");
                }
                else
                {
                    return StatusCode((int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while making the API update request.");
            }
        }
        [HttpDelete]
        [Route("DeleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(long id) // Use a long data type for id
        {
            string accessToken = "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";
            string storeName = "quickstart-e8be2cd1";
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
            var apiUrl = $"https://{storeName}.myshopify.com/admin/api/2023-07/products/{id}.json"; // Use the id in the URL

            var response = await client.DeleteAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return Content(responseContent, "application/json");
            }
            else
            {
                return StatusCode((int)response.StatusCode);
            }
        }

    }
}
public class ShopifyProductViewModel
{
    public string ProductId { get; set; }
    public string Title { get; set; }
    public string VariantId { get; set; }
    public string Price { get; set; }
}
public class Variant
{
    public long id { get; set; }
    public string price { get; set; }
}

public class Product
{
    public long id { get; set; }
    public string title { get; set; }
    public List<Variant> variants { get; set; }
}
public class Root
{
    public Product product { get; set; }
}


