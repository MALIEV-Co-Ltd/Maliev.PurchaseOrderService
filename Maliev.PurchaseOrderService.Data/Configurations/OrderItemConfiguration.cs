using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.PurchaseOrderService.Data.Entities;

namespace Maliev.PurchaseOrderService.Data.Configurations;

/// <summary>
/// Entity configuration for OrderItem entity
/// Configures relationships, constraints, and indexes for order items
/// </summary>
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        // Table configuration
        builder.ToTable("OrderItems");
        builder.HasKey(oi => oi.Id);

        // Primary key and identity
        builder.Property(oi => oi.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Required foreign key properties
        builder.Property(oi => oi.PurchaseOrderId)
            .IsRequired();

        builder.Property(oi => oi.ExternalOrderItemId)
            .IsRequired();

        // String properties with specific lengths
        builder.Property(oi => oi.ProductCode)
            .HasMaxLength(50);

        builder.Property(oi => oi.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(oi => oi.UnitOfMeasure)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(oi => oi.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(oi => oi.Notes)
            .HasMaxLength(500);

        // Decimal properties with precision
        builder.Property(oi => oi.Quantity)
            .IsRequired()
            .HasColumnType("decimal(10,3)");

        builder.Property(oi => oi.UnitPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(oi => oi.TotalPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        // Required timestamp properties
        builder.Property(oi => oi.CachedAt)
            .IsRequired();

        // Boolean properties
        builder.Property(oi => oi.ExternallyModified)
            .IsRequired()
            .HasDefaultValue(false);

        // Unique constraint for external order item reference
        builder.HasIndex(oi => new { oi.PurchaseOrderId, oi.ExternalOrderItemId })
            .IsUnique()
            .HasDatabaseName("IX_OrderItems_PurchaseOrderId_ExternalOrderItemId_Unique");

        // Performance indexes
        builder.HasIndex(oi => oi.PurchaseOrderId)
            .HasDatabaseName("IX_OrderItems_PurchaseOrderId");

        builder.HasIndex(oi => oi.ExternalOrderItemId)
            .HasDatabaseName("IX_OrderItems_ExternalOrderItemId");

        builder.HasIndex(oi => oi.ProductCode)
            .HasDatabaseName("IX_OrderItems_ProductCode");

        builder.HasIndex(oi => oi.CachedAt)
            .HasDatabaseName("IX_OrderItems_CachedAt");

        builder.HasIndex(oi => oi.ExternallyModified)
            .HasDatabaseName("IX_OrderItems_ExternallyModified");

        // Composite indexes for common query patterns
        builder.HasIndex(oi => new { oi.PurchaseOrderId, oi.ProductCode })
            .HasDatabaseName("IX_OrderItems_PurchaseOrderId_ProductCode");

        builder.HasIndex(oi => new { oi.ExternallyModified, oi.CachedAt })
            .HasDatabaseName("IX_OrderItems_ExternallyModified_CachedAt");

        // Configure relationship with PurchaseOrder
        builder.HasOne(oi => oi.PurchaseOrder)
            .WithMany(po => po.OrderItems)
            .HasForeignKey(oi => oi.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}