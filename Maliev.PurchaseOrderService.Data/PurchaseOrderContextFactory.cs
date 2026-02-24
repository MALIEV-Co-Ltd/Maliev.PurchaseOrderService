using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.PurchaseOrderService.Data;

/// <summary>
/// Design-time factory for creating PurchaseOrderContext instances during migrations
/// </summary>
public class PurchaseOrderContextFactory : IDesignTimeDbContextFactory<PurchaseOrderContext>
{
    /// <summary>
    /// Creates a new instance of a design-time DbContext.
    /// </summary>
    /// <param name="args">Arguments provided by the design-time services.</param>
    /// <returns>A new <see cref="PurchaseOrderContext"/> instance.</returns>
    public PurchaseOrderContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptions<PurchaseOrderContext>();

        // Use a placeholder connection string for migrations
        // The actual connection string will be configured at runtime via environment variables
        var builder = new DbContextOptionsBuilder<PurchaseOrderContext>();
        builder.UseNpgsql("Host=localhost;Database=purchaseorder_app_db;Username=postgres;Password=postgres");

        return new PurchaseOrderContext(builder.Options);
    }
}
