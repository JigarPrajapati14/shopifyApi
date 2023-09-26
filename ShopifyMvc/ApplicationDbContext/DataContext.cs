using Microsoft.EntityFrameworkCore;
using ShopifyMvc.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ShopifyMvc.ApplicationDbContext
{
    public class DataContext : DbContext
    {
        public DbSet<shopify> ShopifyProduct { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseCosmos(
                "https://jigarcosmos.documents.azure.com:443/",
                "fprrOdisE7evscpVA4uqVH6ZBsGPLBmM95Q2ksXvejppBqWqCDpO3oS48uQnIxx1N3mbqcc5r3B5ACDbW1IZ8w==",
                "product-db");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<shopify>()
                .ToContainer("ShopifyProduct")
                .HasPartitionKey(e => e.ProductId);
        }
    }
}
