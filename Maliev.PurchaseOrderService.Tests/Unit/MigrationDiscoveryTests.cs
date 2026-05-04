using Maliev.PurchaseOrderService.Infrastructure.Migrations;
using Maliev.PurchaseOrderService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Maliev.PurchaseOrderService.Tests.Unit;

public class MigrationDiscoveryTests
{
    [Fact]
    public void AddExternalPurchaseOrderReferences_HasEfMigrationMetadata()
    {
        var migration = typeof(AddExternalPurchaseOrderReferences)
            .GetCustomAttributes(typeof(MigrationAttribute), inherit: false)
            .OfType<MigrationAttribute>()
            .SingleOrDefault();

        var dbContext = typeof(AddExternalPurchaseOrderReferences)
            .GetCustomAttributes(typeof(DbContextAttribute), inherit: false)
            .OfType<DbContextAttribute>()
            .SingleOrDefault();

        Assert.NotNull(migration);
        Assert.Equal("20260503120000_AddExternalPurchaseOrderReferences", migration.Id);
        Assert.NotNull(dbContext);
        Assert.Equal(typeof(PurchaseOrderContext), dbContext.ContextType);
    }
}
