using ShopifyMvc.ApplicationDbContext;
using ShopifyMvc.Service;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json");

var cosmosConfig = builder.Configuration.GetSection("CosmosDb");
var cosmosEndpoint = cosmosConfig["Endpoint"];
var cosmosKey = cosmosConfig["Key"];

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddDbContext<DataContext>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(30);
});
var configuration = builder.Configuration;
builder.Services.AddScoped<ShopifyService>(provider =>
{
    var databaseName = "product-db";
    var containerName = "Shopify";
    var dbContext = provider.GetRequiredService<DataContext>();

    return new ShopifyService(dbContext, configuration);
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ShopifyMvc}/{action=Index}/{id?}");

app.Run();
