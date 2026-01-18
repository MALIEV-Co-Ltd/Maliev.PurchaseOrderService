using Maliev.Aspire.ServiceDefaults.Database;
using Maliev.PurchaseOrderService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maliev.PurchaseOrderService.Data;

/// <summary>
/// Database context for the Purchase Order Service
/// </summary>
public class PurchaseOrderContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PurchaseOrderContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public PurchaseOrderContext(DbContextOptions<PurchaseOrderContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the PurchaseOrders DbSet.
    /// </summary>
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    /// <summary>
    /// Gets or sets the OrderItems DbSet.
    /// </summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <summary>
    /// Gets or sets the Addresses DbSet.
    /// </summary>
    public DbSet<Address> Addresses => Set<Address>();

    /// <summary>
    /// Gets or sets the PurchaseOrderFiles DbSet.
    /// </summary>
    public DbSet<PurchaseOrderFile> PurchaseOrderFiles => Set<PurchaseOrderFile>();

    /// <summary>
    /// Gets or sets the AuditLogs DbSet.
    /// </summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>
    /// Gets or sets the DomainEvents DbSet.
    /// </summary>
    public DbSet<DomainEvent> DomainEvents => Set<DomainEvent>();

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    /// <summary>
    /// Configures the schema needed for the identity framework.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PurchaseOrder configuration
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("PurchaseOrders");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OrderNumber)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasIndex(e => e.OrderNumber)
                .IsUnique()
                .HasFilter("\"is_deleted\" = false");

            entity.Property(e => e.CustomerPO)
                .HasMaxLength(50);

            entity.Property(e => e.SupplierName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.SupplierContactInfo)
                .HasMaxLength(200);

            entity.Property(e => e.CurrencyCode)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(e => e.CurrencySymbol)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.SubtotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.WHTRate)
                .HasColumnType("decimal(5,2)");

            entity.Property(e => e.WHTAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.CreatedBy)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.LastModifiedBy)
                .HasMaxLength(50);

            entity.Property(e => e.ApprovedBy)
                .HasMaxLength(50);

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            entity.Property(e => e.DeletedBy)
                .HasMaxLength(50);

            // Optimistic concurrency using xmin for PostgreSQL
            entity.Property(e => e.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            // Indexes for performance
            entity.HasIndex(e => e.SupplierID);

            entity.HasIndex(e => e.OrderID);

            entity.HasIndex(e => new { e.CreatedBy, e.Status });

            entity.HasIndex(e => e.CreatedAt);

            entity.HasIndex(e => new { e.Status, e.OrderType });

            // Relationships
            entity.HasMany(e => e.Items)
                .WithOne(e => e.PurchaseOrder)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ShippingAddress)
                .WithMany()
                .HasForeignKey(e => e.ShippingAddressId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.BillingAddress)
                .WithMany()
                .HasForeignKey(e => e.BillingAddressId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Files)
                .WithOne(e => e.PurchaseOrder)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Soft delete query filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProductCode)
                .HasMaxLength(50);

            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Quantity)
                .HasColumnType("decimal(10,3)");

            entity.Property(e => e.UnitOfMeasure)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TotalPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(e => e.Notes)
                .HasMaxLength(500);

            // Indexes
            entity.HasIndex(e => e.PurchaseOrderId);

            entity.HasIndex(e => e.ProductCode);

            // Matching query filter to avoid warnings with required relationship
            entity.HasQueryFilter(e => !e.PurchaseOrder.IsDeleted);
        });

        // Address configuration
        modelBuilder.Entity<Address>(entity =>
        {
            entity.ToTable("Addresses");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CompanyName)
                .HasMaxLength(100);

            entity.Property(e => e.ContactName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.AddressLine1)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.AddressLine2)
                .HasMaxLength(100);

            entity.Property(e => e.City)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.StateProvince)
                .HasMaxLength(50);

            entity.Property(e => e.PostalCode)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Country)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(e => e.EmailAddress)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedBy)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.LastModifiedBy)
                .HasMaxLength(50);
        });

        // PurchaseOrderFile configuration
        modelBuilder.Entity<PurchaseOrderFile>(entity =>
        {
            entity.ToTable("PurchaseOrderFiles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.ObjectName)
                .IsRequired()
                .HasMaxLength(500);

            entity.HasIndex(e => e.ObjectName)
                .IsUnique();

            entity.Property(e => e.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.UploadedBy)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            // Indexes
            entity.HasIndex(e => e.PurchaseOrderId);

            entity.HasIndex(e => e.UploadedBy);

            entity.HasIndex(e => e.DocumentType);

            // Soft delete query filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EntityType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.EntityId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.UserRole)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.ExternalServiceName)
                .HasMaxLength(50);

            entity.Property(e => e.IPAddress)
                .HasMaxLength(45);

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500);

            entity.Property(e => e.ChangeReason)
                .HasMaxLength(200);

            // Indexes
            entity.HasIndex(e => new { e.EntityType, e.EntityId });

            entity.HasIndex(e => new { e.UserId, e.Timestamp });

            entity.HasIndex(e => e.Timestamp);
        });

        // DomainEvent configuration
        modelBuilder.Entity<DomainEvent>(entity =>
        {
            entity.ToTable("DomainEvents");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.AggregateId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.AggregateType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.EventData)
                .IsRequired();

            entity.Property(e => e.CorrelationId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.LastProcessingError)
                .HasMaxLength(1000);

            // Indexes
            entity.HasIndex(e => new { e.IsProcessed, e.OccurredAt });

            entity.HasIndex(e => new { e.EventType, e.AggregateId });

            entity.HasIndex(e => e.CorrelationId);

            entity.HasIndex(e => e.ProcessingAttempts);
        });

        // Apply PostgreSQL snake_case naming convention globally
        SnakeCaseNamingHelper.ApplySnakeCaseNaming(modelBuilder);
    }
}
