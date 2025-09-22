using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Data.Configurations;

/// <summary>
/// Entity configuration for PurchaseOrder entity
/// Configures relationships, constraints, indexes, and optimistic concurrency
/// </summary>
public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        // Table configuration
        builder.ToTable("PurchaseOrders");
        builder.HasKey(po => po.Id);

        // Primary key and identity
        builder.Property(po => po.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Required string properties with specific lengths
        builder.Property(po => po.OrderNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(po => po.CustomerPO)
            .HasMaxLength(50);

        builder.Property(po => po.SupplierName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(po => po.SupplierContactInfo)
            .HasMaxLength(200);

        builder.Property(po => po.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(po => po.CurrencySymbol)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(po => po.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(po => po.CreatedBy)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(po => po.UpdatedBy)
            .HasMaxLength(50);

        builder.Property(po => po.ApprovedBy)
            .HasMaxLength(50);

        builder.Property(po => po.Notes)
            .HasMaxLength(1000);

        builder.Property(po => po.PdfGeneratedBy)
            .HasMaxLength(50);

        // Required foreign key properties
        builder.Property(po => po.SupplierID)
            .IsRequired();

        builder.Property(po => po.OrderID)
            .IsRequired();

        builder.Property(po => po.CurrencyID)
            .IsRequired();

        // Required date/time properties
        builder.Property(po => po.OrderDate)
            .IsRequired();

        builder.Property(po => po.CreatedAt)
            .IsRequired();

        // Enum properties
        builder.Property(po => po.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(po => po.OrderType)
            .IsRequired()
            .HasConversion<int>();

        // Decimal properties with precision
        builder.Property(po => po.SubtotalAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(po => po.WHTRate)
            .HasColumnType("decimal(5,2)");

        builder.Property(po => po.WHTAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(po => po.TotalAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        // Optimistic concurrency control
        builder.Property(po => po.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // PDF generation properties
        builder.Property(po => po.IsPdfGenerationEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(po => po.IsPdfGenerated)
            .IsRequired()
            .HasDefaultValue(false);

        // Soft delete property
        builder.Property(po => po.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Unique constraints
        builder.HasIndex(po => po.OrderNumber)
            .IsUnique()
            .HasDatabaseName("IX_PurchaseOrders_OrderNumber_Unique");

        // Performance indexes
        builder.HasIndex(po => po.Status)
            .HasDatabaseName("IX_PurchaseOrders_Status");

        builder.HasIndex(po => po.OrderType)
            .HasDatabaseName("IX_PurchaseOrders_OrderType");

        builder.HasIndex(po => po.SupplierID)
            .HasDatabaseName("IX_PurchaseOrders_SupplierID");

        builder.HasIndex(po => po.OrderID)
            .HasDatabaseName("IX_PurchaseOrders_OrderID");

        builder.HasIndex(po => po.CreatedAt)
            .HasDatabaseName("IX_PurchaseOrders_CreatedAt");

        builder.HasIndex(po => po.OrderDate)
            .HasDatabaseName("IX_PurchaseOrders_OrderDate");

        // Composite indexes for common query patterns
        builder.HasIndex(po => new { po.Status, po.OrderType })
            .HasDatabaseName("IX_PurchaseOrders_Status_OrderType");

        builder.HasIndex(po => new { po.SupplierID, po.Status })
            .HasDatabaseName("IX_PurchaseOrders_SupplierID_Status");

        builder.HasIndex(po => new { po.CreatedAt, po.Status })
            .HasDatabaseName("IX_PurchaseOrders_CreatedAt_Status");

        // Soft delete index
        builder.HasIndex(po => po.IsDeleted)
            .HasDatabaseName("IX_PurchaseOrders_IsDeleted");

        // Configure relationships
        builder.HasMany(po => po.OrderItems)
            .WithOne(oi => oi.PurchaseOrder)
            .HasForeignKey(oi => oi.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(po => po.PurchaseOrderFiles)
            .WithOne(pof => pof.PurchaseOrder)
            .HasForeignKey(pof => pof.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional address relationships
        builder.HasOne(po => po.ShippingAddress)
            .WithMany(a => a.ShippingPurchaseOrders)
            .HasForeignKey(po => po.ShippingAddressId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(po => po.BillingAddress)
            .WithMany(a => a.BillingPurchaseOrders)
            .HasForeignKey(po => po.BillingAddressId)
            .OnDelete(DeleteBehavior.SetNull);

        // Generated PDF file relationship
        builder.HasOne(po => po.GeneratedPdfFile)
            .WithMany()
            .HasForeignKey(po => po.GeneratedPdfFileId)
            .OnDelete(DeleteBehavior.SetNull);

        // Note: DomainEvents are loosely coupled and queried by AggregateId (no navigation property)

        // Note: AuditLogs are loosely coupled and queried by EntityId (no navigation property)
    }
}