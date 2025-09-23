using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Data.Configurations;

/// <summary>
/// Entity configuration for Address entity
/// Configures relationships, constraints, and indexes for addresses
/// </summary>
public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        // Table configuration
        builder.ToTable("Addresses", t =>
        {
            // Email validation constraint
            t.HasCheckConstraint("CK_Addresses_EmailAddress_Format",
                "\"EmailAddress\" IS NULL OR \"EmailAddress\" ~ '^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$'");
        });
        builder.HasKey(a => a.Id);

        // Primary key and identity
        builder.Property(a => a.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Enum properties
        builder.Property(a => a.AddressType)
            .IsRequired()
            .HasConversion<int>();

        // String properties with specific lengths
        builder.Property(a => a.CompanyName)
            .HasMaxLength(100);

        builder.Property(a => a.ContactName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.AddressLine1)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.AddressLine2)
            .HasMaxLength(100);

        builder.Property(a => a.City)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.StateProvince)
            .HasMaxLength(50);

        builder.Property(a => a.PostalCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.Country)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(a => a.EmailAddress)
            .HasMaxLength(100);

        builder.Property(a => a.CreatedBy)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.UpdatedBy)
            .HasMaxLength(50);

        // Boolean properties with defaults
        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.IsValidated)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Required timestamp properties
        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // Optimistic concurrency control
        builder.Property(a => a.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();


        // Performance indexes
        builder.HasIndex(a => a.AddressType)
            .HasDatabaseName("IX_Addresses_AddressType");

        builder.HasIndex(a => a.Country)
            .HasDatabaseName("IX_Addresses_Country");

        builder.HasIndex(a => a.City)
            .HasDatabaseName("IX_Addresses_City");

        builder.HasIndex(a => a.PostalCode)
            .HasDatabaseName("IX_Addresses_PostalCode");

        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_Addresses_CreatedAt");

        // Composite indexes for common query patterns
        builder.HasIndex(a => new { a.Country, a.City })
            .HasDatabaseName("IX_Addresses_Country_City");

        builder.HasIndex(a => new { a.AddressType, a.Country })
            .HasDatabaseName("IX_Addresses_AddressType_Country");

        // Soft delete index
        builder.HasIndex(a => a.IsDeleted)
            .HasDatabaseName("IX_Addresses_IsDeleted");

        // Configure relationships with PurchaseOrder
        builder.HasMany(a => a.ShippingPurchaseOrders)
            .WithOne(po => po.ShippingAddress)
            .HasForeignKey(po => po.ShippingAddressId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(a => a.BillingPurchaseOrders)
            .WithOne(po => po.BillingAddress)
            .HasForeignKey(po => po.BillingAddressId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}