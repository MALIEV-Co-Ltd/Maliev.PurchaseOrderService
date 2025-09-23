using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Data.Configurations;

/// <summary>
/// Entity configuration for PurchaseOrderFile entity
/// Configures relationships, constraints, and indexes for file references
/// </summary>
public class PurchaseOrderFileConfiguration : IEntityTypeConfiguration<PurchaseOrderFile>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderFile> builder)
    {
        // Table configuration
        builder.ToTable("PurchaseOrderFiles", t =>
        {
            // Check constraints
            t.HasCheckConstraint("CK_PurchaseOrderFiles_FileSize_Positive",
                "\"FileSize\" > 0");
        });
        builder.HasKey(pof => pof.Id);

        // Primary key and identity
        builder.Property(pof => pof.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        // Required foreign key properties
        builder.Property(pof => pof.PurchaseOrderId)
            .IsRequired();

        // String properties with specific lengths
        builder.Property(pof => pof.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(pof => pof.ObjectName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(pof => pof.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pof => pof.UploadedBy)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pof => pof.Description)
            .HasMaxLength(500);

        builder.Property(pof => pof.VirusScanStatus)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Pending");

        builder.Property(pof => pof.FileHash)
            .HasMaxLength(32);

        builder.Property(pof => pof.ExternalUrl)
            .HasMaxLength(1000);

        builder.Property(pof => pof.LastDownloadedBy)
            .HasMaxLength(50);

        builder.Property(pof => pof.UpdatedBy)
            .HasMaxLength(50);

        // Enum properties
        builder.Property(pof => pof.DocumentType)
            .IsRequired()
            .HasConversion<int>();

        // Numeric properties
        builder.Property(pof => pof.FileSize)
            .IsRequired();

        // Required timestamp properties
        builder.Property(pof => pof.UploadedAt)
            .IsRequired();

        // Boolean properties with defaults
        builder.Property(pof => pof.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(pof => pof.IsSystemGenerated)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(pof => pof.DownloadCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Soft delete property
        builder.Property(pof => pof.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Optimistic concurrency control
        builder.Property(pof => pof.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Unique constraint for object name to prevent duplicates
        builder.HasIndex(pof => pof.ObjectName)
            .IsUnique()
            .HasDatabaseName("IX_PurchaseOrderFiles_ObjectName_Unique");

        // Performance indexes
        builder.HasIndex(pof => pof.PurchaseOrderId)
            .HasDatabaseName("IX_PurchaseOrderFiles_PurchaseOrderId");

        builder.HasIndex(pof => pof.DocumentType)
            .HasDatabaseName("IX_PurchaseOrderFiles_DocumentType");

        builder.HasIndex(pof => pof.UploadedBy)
            .HasDatabaseName("IX_PurchaseOrderFiles_UploadedBy");

        builder.HasIndex(pof => pof.UploadedAt)
            .HasDatabaseName("IX_PurchaseOrderFiles_UploadedAt");

        builder.HasIndex(pof => pof.ContentType)
            .HasDatabaseName("IX_PurchaseOrderFiles_ContentType");

        // Soft delete index
        builder.HasIndex(pof => pof.IsDeleted)
            .HasDatabaseName("IX_PurchaseOrderFiles_IsDeleted");

        // Composite indexes for common query patterns
        builder.HasIndex(pof => new { pof.PurchaseOrderId, pof.DocumentType })
            .HasDatabaseName("IX_PurchaseOrderFiles_PurchaseOrderId_DocumentType");

        builder.HasIndex(pof => new { pof.UploadedAt, pof.DocumentType })
            .HasDatabaseName("IX_PurchaseOrderFiles_UploadedAt_DocumentType");

        builder.HasIndex(pof => new { pof.IsDeleted, pof.PurchaseOrderId })
            .HasDatabaseName("IX_PurchaseOrderFiles_IsDeleted_PurchaseOrderId");

        // Configure relationship with PurchaseOrder
        builder.HasOne(pof => pof.PurchaseOrder)
            .WithMany(po => po.PurchaseOrderFiles)
            .HasForeignKey(pof => pof.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}