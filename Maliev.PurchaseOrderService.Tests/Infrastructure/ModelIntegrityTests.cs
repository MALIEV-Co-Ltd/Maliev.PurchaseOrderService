using Maliev.PurchaseOrderService.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.PurchaseOrderService.Tests.Infrastructure;

public class ModelIntegrityTests
{
    [Fact]
    public void Model_ShouldNotHavePendingChanges()
    {
        var options = new DbContextOptionsBuilder<PurchaseOrderContext>()
            .UseNpgsql("Host=localhost;Database=ModelCheck")
            .Options;

        using var context = new PurchaseOrderContext(options);
        var hasChanges = context.Database.HasPendingModelChanges();

        Assert.False(hasChanges, "Run 'dotnet ef migrations add <Name> --project Maliev.PurchaseOrderService.Data --startup-project Maliev.PurchaseOrderService.Api'");
    }
}
