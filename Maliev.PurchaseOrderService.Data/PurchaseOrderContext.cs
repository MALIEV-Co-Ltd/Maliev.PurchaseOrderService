namespace Maliev.PurchaseOrderService.Data
{
    using Microsoft.EntityFrameworkCore;
    using Maliev.PurchaseOrderService.Data.Entities;

    public partial class PurchaseOrderContext : DbContext
    {
        public PurchaseOrderContext(DbContextOptions<PurchaseOrderContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Address> Address { get; set; }
        public virtual DbSet<OrderItem> OrderItem { get; set; }
        public virtual DbSet<PurchaseOrder> PurchaseOrder { get; set; }
        public virtual DbSet<PurchaseOrderFile> PurchaseOrderFile { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Address>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.AddressLine1)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.AddressLine2).HasMaxLength(256);

                entity.Property(e => e.Building).HasMaxLength(256);

                entity.Property(e => e.City).HasMaxLength(256);

                entity.Property(e => e.CountryId).HasColumnName("CountryID");

                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.ModifiedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.PostalCode).HasMaxLength(256);

                entity.Property(e => e.State).HasMaxLength(256);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.ModifiedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.PartNumber).HasMaxLength(100);

                entity.Property(e => e.PurchaseOrderId).HasColumnName("PurchaseOrderID");

                entity.Property(e => e.Subtotal)
                    .HasColumnType("decimal(18, 2)")
                    .HasComputedColumnSql("(CONVERT([decimal](18,2),[UnitPrice]*[Quantity]))");

                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.PurchaseOrder)
                    .WithMany(p => p.OrderItem)
                    .HasForeignKey(d => d.PurchaseOrderId)
                    .HasConstraintName("FK_OrderItem_PurchaseOrder");
            });

            modelBuilder.Entity<PurchaseOrder>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.BillingAddressId).HasColumnName("BillingAddressID");

                entity.Property(e => e.BillingContactPerson).HasMaxLength(256);

                entity.Property(e => e.BillingFax).HasMaxLength(256);

                entity.Property(e => e.BillingMobile).HasMaxLength(256);

                entity.Property(e => e.BillingTelephone).HasMaxLength(256);

                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");

                entity.Property(e => e.Fob).HasColumnName("FOB");

                entity.Property(e => e.ModifiedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.ShippingAddressId).HasColumnName("ShippingAddressID");

                entity.Property(e => e.ShippingContactPerson).HasMaxLength(256);

                entity.Property(e => e.ShippingFax).HasMaxLength(256);

                entity.Property(e => e.ShippingMethod).HasMaxLength(256);

                entity.Property(e => e.ShippingMobile).HasMaxLength(256);

                entity.Property(e => e.ShippingTelephone).HasMaxLength(256);

                entity.Property(e => e.SupplierContactPerson).HasMaxLength(256);

                entity.Property(e => e.SupplierId).HasColumnName("SupplierID");

                entity.Property(e => e.Terms).HasMaxLength(256);

                entity.HasOne(d => d.BillingAddress)
                    .WithMany(p => p.PurchaseOrderBillingAddress)
                    .HasForeignKey(d => d.BillingAddressId)
                    .HasConstraintName("FK_PurchaseOrder_Address1");

                entity.HasOne(d => d.ShippingAddress)
                    .WithMany(p => p.PurchaseOrderShippingAddress)
                    .HasForeignKey(d => d.ShippingAddressId)
                    .HasConstraintName("FK_PurchaseOrder_Address");
            });

            modelBuilder.Entity<PurchaseOrderFile>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Bucket)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.ModifiedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.ObjectName).IsRequired();

                entity.Property(e => e.PurchaseOrderId).HasColumnName("PurchaseOrderID");

                entity.HasOne(d => d.PurchaseOrder)
                    .WithMany(p => p.PurchaseOrderFile)
                    .HasForeignKey(d => d.PurchaseOrderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PurchaseOrderFile_PurchaseOrder");
            });
        }
    }
}
