using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.PurchaseOrderService.Data;

/// <summary>
/// Design-time factory for creating PurchaseOrderContext during migrations
/// </summary>
public class PurchaseOrderContextFactory : IDesignTimeDbContextFactory<PurchaseOrderContext>
{
    public PurchaseOrderContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PurchaseOrderContext>();

        // Use environment variable for design-time migrations
        // Set DESIGN_TIME_CONNECTION_STRING environment variable for migrations
        // Example: DESIGN_TIME_CONNECTION_STRING="Host=localhost;Database=purchaseorder_app_db;Username=postgres;Password=your_password"
        var connectionString = Environment.GetEnvironmentVariable("DESIGN_TIME_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__PurchaseOrderDbContext")
            ?? throw new InvalidOperationException(
                "Design-time connection string not found. Please set DESIGN_TIME_CONNECTION_STRING environment variable. " +
                "Example: DESIGN_TIME_CONNECTION_STRING=\"Host=localhost;Database=purchaseorder_app_db;Username=postgres;Password=your_password\"");

        optionsBuilder.UseNpgsql(connectionString);

        return new PurchaseOrderContext(optionsBuilder.Options);
    }
}