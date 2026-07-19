using Maliev.PurchaseOrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Maliev.PurchaseOrderService.Application.Interfaces;

public interface IPurchaseOrderDbContext
{
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Address> Addresses { get; }
    DbSet<PurchaseOrderFile> Files { get; }

    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
