using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Maliev.PurchaseOrderService.Data.Entities;

namespace Maliev.PurchaseOrderService.Data.Configurations;

/// <summary>
/// Entity configuration for DomainEvent entity
/// Configures relationships, constraints, and indexes for domain events
/// </summary>
public class DomainEventConfiguration : IEntityTypeConfiguration<DomainEvent>
{
    public void Configure(EntityTypeBuilder<DomainEvent> builder)
    {
        // Table configuration
        builder.ToTable("DomainEvents", t =>
        {
            // Check constraints
            t.HasCheckConstraint("CK_DomainEvents_ProcessingAttempts_NonNegative",
                "\"ProcessingAttempts\" >= 0");
            t.HasCheckConstraint("CK_DomainEvents_EventVersion_Positive",
                "\"EventVersion\" > 0");
        });
        builder.HasKey(de => de.Id);

        // Primary key and identity
        builder.Property(de => de.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        // String properties with specific lengths
        builder.Property(de => de.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(de => de.AggregateId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(de => de.AggregateType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(de => de.CorrelationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(de => de.UserId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(de => de.LastProcessingError)
            .HasMaxLength(1000);

        // Text properties for JSON data
        builder.Property(de => de.EventData)
            .IsRequired()
            .HasColumnType("text");

        // Numeric properties
        builder.Property(de => de.EventVersion)
            .IsRequired();

        builder.Property(de => de.ProcessingAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        // Required timestamp properties
        builder.Property(de => de.OccurredAt)
            .IsRequired();

        // Boolean properties
        builder.Property(de => de.IsProcessed)
            .IsRequired()
            .HasDefaultValue(false);

        // Performance indexes
        builder.HasIndex(de => de.EventType)
            .HasDatabaseName("IX_DomainEvents_EventType");

        builder.HasIndex(de => de.AggregateId)
            .HasDatabaseName("IX_DomainEvents_AggregateId");

        builder.HasIndex(de => de.AggregateType)
            .HasDatabaseName("IX_DomainEvents_AggregateType");

        builder.HasIndex(de => de.CorrelationId)
            .HasDatabaseName("IX_DomainEvents_CorrelationId");

        builder.HasIndex(de => de.UserId)
            .HasDatabaseName("IX_DomainEvents_UserId");

        builder.HasIndex(de => de.OccurredAt)
            .HasDatabaseName("IX_DomainEvents_OccurredAt");

        builder.HasIndex(de => de.IsProcessed)
            .HasDatabaseName("IX_DomainEvents_IsProcessed");

        // Composite indexes for common query patterns
        builder.HasIndex(de => new { de.AggregateType, de.AggregateId })
            .HasDatabaseName("IX_DomainEvents_AggregateType_AggregateId");

        builder.HasIndex(de => new { de.EventType, de.OccurredAt })
            .HasDatabaseName("IX_DomainEvents_EventType_OccurredAt");

        builder.HasIndex(de => new { de.IsProcessed, de.OccurredAt })
            .HasDatabaseName("IX_DomainEvents_IsProcessed_OccurredAt");

        builder.HasIndex(de => new { de.IsProcessed, de.ProcessingAttempts })
            .HasDatabaseName("IX_DomainEvents_IsProcessed_ProcessingAttempts");

        // Specific index for event processing (unprocessed events ordered by occurrence)
        builder.HasIndex(de => new { de.IsProcessed, de.ProcessingAttempts, de.OccurredAt })
            .HasDatabaseName("IX_DomainEvents_Processing_Queue")
            .HasFilter("\"IsProcessed\" = false");

        // Index for correlation tracking
        builder.HasIndex(de => new { de.CorrelationId, de.OccurredAt })
            .HasDatabaseName("IX_DomainEvents_CorrelationId_OccurredAt");

        // Index for event replay/sourcing
        builder.HasIndex(de => new { de.AggregateType, de.AggregateId, de.OccurredAt })
            .HasDatabaseName("IX_DomainEvents_AggregateType_AggregateId_OccurredAt");

    }
}