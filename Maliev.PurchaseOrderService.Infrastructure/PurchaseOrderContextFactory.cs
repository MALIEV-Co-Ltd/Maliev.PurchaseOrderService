using Maliev.PurchaseOrderService.Infrastructure.Persistence;
using Maliev.PurchaseOrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.PurchaseOrderService.Infrastructure;

public class PurchaseOrderContextFactory : IDesignTimeDbContextFactory<PurchaseOrderContext>
{
    public PurchaseOrderContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PurchaseOrderContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=maliev_purchase_orders;Username=postgres;Password=postgres");

        return new PurchaseOrderContext(optionsBuilder.Options);
    }
}
