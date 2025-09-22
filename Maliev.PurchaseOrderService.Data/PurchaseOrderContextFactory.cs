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

        // Use a temporary connection string for migrations
        // This will be replaced by environment variables in runtime
        optionsBuilder.UseNpgsql("Host=localhost;Database=purchaseorder_design_db;Username=postgres;Password=postgres");

        return new PurchaseOrderContext(optionsBuilder.Options);
    }
}