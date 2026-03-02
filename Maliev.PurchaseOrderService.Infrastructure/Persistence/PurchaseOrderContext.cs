using Maliev.PurchaseOrderService.Application.Interfaces;
using Maliev.PurchaseOrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Maliev.PurchaseOrderService.Infrastructure.Persistence;

public class PurchaseOrderContext : DbContext, IPurchaseOrderDbContext
{
    public PurchaseOrderContext(DbContextOptions<PurchaseOrderContext> options) : base(options)
    {
    }

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<PurchaseOrderFile> Files => Set<PurchaseOrderFile>();

    DatabaseFacade IPurchaseOrderDbContext.Database => base.Database;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
