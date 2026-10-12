
using Catalogue.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalogue.Api.Data;

public class CatalogueDbContext : DbContext
{
    public CatalogueDbContext(
        DbContextOptions<CatalogueDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .HasIndex(product => product.SKU)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .Property(product => product.Price)
            .HasPrecision(10, 2);
    }
}
