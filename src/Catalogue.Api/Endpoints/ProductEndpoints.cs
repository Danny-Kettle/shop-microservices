
using System.ComponentModel.DataAnnotations;
using Catalogue.Api.Data;
using Catalogue.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalogue.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(
        this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/products")
            .WithTags("Products");

        products.MapGet("/", async (CatalogueDbContext db) =>
            Results.Ok(await db.Products
                .AsNoTracking()
                .ToListAsync()));

        products.MapGet("/{id:int}", async (
            int id,
            CatalogueDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);

            return product is null
                ? Results.NotFound()
                : Results.Ok(product);
        });

        products.MapPost("/", async (
            Product product,
            CatalogueDbContext db) =>
        {
            var errors = ValidateProduct(product);

            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var skuExists = await db.Products
                .AnyAsync(p => p.SKU == product.SKU);

            if (skuExists)
                return Results.Conflict(new
                {
                    message = "A product with this SKU already exists."
                });

            product.Id = 0;

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/products/{product.Id}", product);
        });

        products.MapPut("/{id:int}", async (
            int id,
            Product updatedProduct,
            CatalogueDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);

            if (product is null)
                return Results.NotFound();

            var errors = ValidateProduct(updatedProduct);

            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var skuExists = await db.Products.AnyAsync(
                p => p.SKU == updatedProduct.SKU && p.Id != id);

            if (skuExists)
                return Results.Conflict(new
                {
                    message = "A product with this SKU already exists."
                });

            product.Name = updatedProduct.Name;
            product.Description = updatedProduct.Description;
            product.SKU = updatedProduct.SKU;
            product.Price = updatedProduct.Price;
            product.StockQuantity = updatedProduct.StockQuantity;

            await db.SaveChangesAsync();

            return Results.Ok(product);
        });

        products.MapDelete("/{id:int}", async (
            int id,
            CatalogueDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);

            if (product is null)
                return Results.NotFound();

            db.Products.Remove(product);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return app;
    }

    private static Dictionary<string, string[]> ValidateProduct(
        Product product)
    {
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            product,
            new ValidationContext(product),
            results,
            validateAllProperties: true);

        return results
            .SelectMany(result =>
                (result.MemberNames.Any()
                    ? result.MemberNames
                    : new[] { string.Empty })
                .Select(member => new
                {
                    Member = member,
                    Message = result.ErrorMessage ?? "Invalid value."
                }))
            .GroupBy(error => error.Member)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Message).ToArray());
    }
}
