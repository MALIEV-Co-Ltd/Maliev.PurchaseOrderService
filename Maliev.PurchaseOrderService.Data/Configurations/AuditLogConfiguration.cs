using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Data.Configurations;

/// <summary>
/// Entity configuration for AuditLog entity
/// Configures relationships, constraints, and indexes for audit trail
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Table configuration
        builder.ToTable("AuditLogs");
        builder.HasKey(al => al.Id);

        // Primary key and identity
        builder.Property(al => al.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        // String properties with specific lengths
        builder.Property(al => al.EntityType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(al => al.EntityId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(al => al.UserId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(al => al.UserRole)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(al => al.ExternalServiceName)
            .HasMaxLength(50);

        builder.Property(al => al.IPAddress)
            .HasMaxLength(45); // IPv6 support

        builder.Property(al => al.UserAgent)
            .HasMaxLength(500);

        builder.Property(al => al.ChangeReason)
            .HasMaxLength(200);

        // Enum properties
        builder.Property(al => al.Action)
            .IsRequired()
            .HasConversion<int>();

        // Required timestamp properties
        builder.Property(al => al.Timestamp)
            .IsRequired();

        // Text properties for JSON data
        builder.Property(al => al.OldValues)
            .HasColumnType("text");

        builder.Property(al => al.NewValues)
            .HasColumnType("text");

        builder.Property(al => al.ExternalServiceResponse)
            .HasColumnType("text");

        // Performance indexes
        builder.HasIndex(al => al.EntityType)
            .HasDatabaseName("IX_AuditLogs_EntityType");

        builder.HasIndex(al => al.EntityId)
            .HasDatabaseName("IX_AuditLogs_EntityId");

        builder.HasIndex(al => al.Action)
            .HasDatabaseName("IX_AuditLogs_Action");

        builder.HasIndex(al => al.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(al => al.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        builder.HasIndex(al => al.ExternalServiceName)
            .HasDatabaseName("IX_AuditLogs_ExternalServiceName");

        // Composite indexes for common query patterns
        builder.HasIndex(al => new { al.EntityType, al.EntityId })
            .HasDatabaseName("IX_AuditLogs_EntityType_EntityId");

        builder.HasIndex(al => new { al.EntityType, al.Action })
            .HasDatabaseName("IX_AuditLogs_EntityType_Action");

        builder.HasIndex(al => new { al.UserId, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_UserId_Timestamp");

        builder.HasIndex(al => new { al.Timestamp, al.Action })
            .HasDatabaseName("IX_AuditLogs_Timestamp_Action");

        builder.HasIndex(al => new { al.EntityType, al.EntityId, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_EntityType_EntityId_Timestamp");

        // Specific index for external service calls
        builder.HasIndex(al => new { al.ExternalServiceName, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_ExternalServiceName_Timestamp")
            .HasFilter("\"ExternalServiceName\" IS NOT NULL");

        // Index for compliance reporting
        builder.HasIndex(al => new { al.UserRole, al.Action, al.Timestamp })
            .HasDatabaseName("IX_AuditLogs_UserRole_Action_Timestamp");
    }
}