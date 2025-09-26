using Microsoft.EntityFrameworkCore;
using Maliev.PurchaseOrderService.Data.Entities;

namespace Maliev.PurchaseOrderService.Data;

/// <summary>
/// Entity Framework DbContext for the Purchase Order Service
/// Manages all entities related to purchase orders, items, addresses, files, audit logs, and domain events
/// </summary>
public class PurchaseOrderContext : DbContext
{
    public PurchaseOrderContext(DbContextOptions<PurchaseOrderContext> options) : base(options)
    {
    }

    /// <summary>
    /// Purchase orders - aggregate root
    /// </summary>
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;

    /// <summary>
    /// Order items derived from external OrderService
    /// </summary>
    public DbSet<OrderItem> OrderItems { get; set; } = null!;

    /// <summary>
    /// Shipping and billing addresses
    /// </summary>
    public DbSet<Address> Addresses { get; set; } = null!;

    /// <summary>
    /// Document references for purchase orders
    /// </summary>
    public DbSet<PurchaseOrderFile> PurchaseOrderFiles { get; set; } = null!;

    /// <summary>
    /// Audit trail for compliance and tracking
    /// </summary>
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    /// <summary>
    /// Domain events for event-driven architecture
    /// </summary>
    public DbSet<DomainEvent> DomainEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PurchaseOrderContext).Assembly);

        // Configure entity relationships
        ConfigureEntityRelationships(modelBuilder);

        // Configure value conversions and additional mappings
        ConfigureValueConversions(modelBuilder);

        // Configure global query filters for soft deletes
        ConfigureGlobalQueryFilters(modelBuilder);
    }

    /// <summary>
    /// Configure entity relationships and foreign keys
    /// </summary>
    private static void ConfigureEntityRelationships(ModelBuilder modelBuilder)
    {
        // PurchaseOrder -> OrderItem (One-to-Many)
        modelBuilder.Entity<PurchaseOrder>()
            .HasMany(po => po.OrderItems)
            .WithOne(oi => oi.PurchaseOrder)
            .HasForeignKey(oi => oi.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(true); // Explicitly specify relationship is required

        // OrderItem -> PurchaseOrder (Many-to-One) - explicit reverse configuration
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.PurchaseOrder)
            .WithMany(po => po.OrderItems)
            .HasForeignKey(oi => oi.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(true);

        // PurchaseOrder -> PurchaseOrderFile (One-to-Many)
        modelBuilder.Entity<PurchaseOrder>()
            .HasMany(po => po.PurchaseOrderFiles)
            .WithOne(pof => pof.PurchaseOrder)
            .HasForeignKey(pof => pof.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(true);

        // PurchaseOrderFile -> PurchaseOrder (Many-to-One) - explicit reverse configuration
        modelBuilder.Entity<PurchaseOrderFile>()
            .HasOne(pof => pof.PurchaseOrder)
            .WithMany(po => po.PurchaseOrderFiles)
            .HasForeignKey(pof => pof.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(true);

        // PurchaseOrder -> Address (Shipping) (Many-to-One, Optional)
        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.ShippingAddress)
            .WithMany(a => a.ShippingPurchaseOrders)
            .HasForeignKey(po => po.ShippingAddressId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // PurchaseOrder -> Address (Billing) (Many-to-One, Optional)
        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(po => po.BillingAddress)
            .WithMany(a => a.BillingPurchaseOrders)
            .HasForeignKey(po => po.BillingAddressId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Configure unique constraints
        modelBuilder.Entity<PurchaseOrder>()
            .HasIndex(po => po.OrderNumber)
            .IsUnique()
            .HasDatabaseName("IX_PurchaseOrders_OrderNumber_Unique");

        // Configure composite indexes for OrderItem
        modelBuilder.Entity<OrderItem>()
            .HasIndex(oi => new { oi.PurchaseOrderId, oi.ExternalOrderItemId })
            .IsUnique()
            .HasDatabaseName("IX_OrderItems_PurchaseOrderId_ExternalOrderItemId_Unique");

        // Configure additional indexes for performance
        modelBuilder.Entity<PurchaseOrder>()
            .HasIndex(po => po.Status)
            .HasDatabaseName("IX_PurchaseOrders_Status");

        modelBuilder.Entity<PurchaseOrder>()
            .HasIndex(po => po.OrderType)
            .HasDatabaseName("IX_PurchaseOrders_OrderType");

        modelBuilder.Entity<Address>()
            .HasIndex(a => a.AddressType)
            .HasDatabaseName("IX_Addresses_AddressType");
    }

    /// <summary>
    /// Configure value conversions and additional mappings
    /// </summary>
    private static void ConfigureValueConversions(ModelBuilder modelBuilder)
    {
        // Ensure consistent UTC handling for DateTime properties
        var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        // Apply to key entities - this ensures consistent UTC handling
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }

    /// <summary>
    /// Configure global query filters for soft deletes
    /// </summary>
    private static void ConfigureGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // Soft delete filter for PurchaseOrder
        modelBuilder.Entity<PurchaseOrder>()
            .HasQueryFilter(po => !po.IsDeleted);

        // Soft delete filter for PurchaseOrderFile
        modelBuilder.Entity<PurchaseOrderFile>()
            .HasQueryFilter(pof => !pof.IsDeleted);

        // Soft delete filter for Address
        modelBuilder.Entity<Address>()
            .HasQueryFilter(a => !a.IsDeleted);

        // Apply matching query filter for OrderItem to resolve navigation warning
        // Since OrderItem doesn't have soft delete, we use a filter that doesn't filter anything
        modelBuilder.Entity<OrderItem>()
            .HasQueryFilter(oi => true);
    }

    /// <summary>
    /// Override SaveChanges to automatically set timestamps and handle soft deletes
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to automatically set timestamps and handle soft deletes
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Automatically update Created/Modified timestamps
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var entity = entry.Entity;
            var now = DateTime.UtcNow;

            // Handle entities with CreatedAt/LastModifiedAt
            if (entry.State == EntityState.Added)
            {
                // Set CreatedAt for new entities
                if (entity.GetType().GetProperty("CreatedAt") != null)
                {
                    entry.Property("CreatedAt").CurrentValue = now;
                }

                // Set OccurredAt for DomainEvent
                if (entity is DomainEvent domainEvent && domainEvent.OccurredAt == default)
                {
                    entry.Property("OccurredAt").CurrentValue = now;
                }

                // Set Timestamp for AuditLog
                if (entity is AuditLog auditLog && auditLog.Timestamp == default)
                {
                    entry.Property("Timestamp").CurrentValue = now;
                }

                // Set CachedAt for OrderItem
                if (entity is OrderItem orderItem && orderItem.CachedAt == default)
                {
                    entry.Property("CachedAt").CurrentValue = now;
                }

                // Set UploadedAt for PurchaseOrderFile
                if (entity is PurchaseOrderFile file && file.UploadedAt == default)
                {
                    entry.Property("UploadedAt").CurrentValue = now;
                }
            }

            if (entry.State == EntityState.Modified)
            {
                // Set UpdatedAt for modified entities
                if (entity.GetType().GetProperty("UpdatedAt") != null)
                {
                    entry.Property("UpdatedAt").CurrentValue = now;
                }
            }
        }
    }
}