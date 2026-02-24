using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.PurchaseOrderService.Data;

/// <summary>
/// Design-time factory for EF Core migrations.
/// Uses environment variable PurchaseOrderContext for connection string.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PurchaseOrderContext>
{
    /// <summary>
    /// Creates a new instance of PurchaseOrderContext for design-time operations.
    /// </summary>
    /// <param name="args">Arguments passed to the factory.</param>
    /// <returns>A configured PurchaseOrderContext instance.</returns>
    public PurchaseOrderContext CreateDbContext(string[] args)
    {
        // Prefer environment variable for connection string, fallback to design-time default if not set
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PurchaseOrderContext")
            ?? "Host=localhost;Database=purchaseorder_design;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<PurchaseOrderContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PurchaseOrderContext(optionsBuilder.Options);
    }
}
