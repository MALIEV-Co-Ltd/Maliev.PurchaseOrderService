using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Maliev.PurchaseOrderService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AddressType = table.Column<int>(type: "integer", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddressLine2 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StateProvince = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EmailAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.CheckConstraint("CK_Addresses_EmailAddress_Format", "\"EmailAddress\" IS NULL OR \"EmailAddress\" ~ '^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$'");
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OldValues = table.Column<string>(type: "text", nullable: true),
                    NewValues = table.Column<string>(type: "text", nullable: true),
                    ExternalServiceName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExternalServiceResponse = table.Column<string>(type: "text", nullable: true),
                    IPAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChangeReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DomainEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AggregateId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventData = table.Column<string>(type: "text", nullable: false),
                    EventVersion = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ProcessingAttempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastProcessingError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainEvents", x => x.Id);
                    table.CheckConstraint("CK_DomainEvents_EventVersion_Positive", "\"EventVersion\" > 0");
                    table.CheckConstraint("CK_DomainEvents_ProcessingAttempts_NonNegative", "\"ProcessingAttempts\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomerPO = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SupplierID = table.Column<int>(type: "integer", nullable: false),
                    OrderID = table.Column<int>(type: "integer", nullable: false),
                    CurrencyID = table.Column<int>(type: "integer", nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SupplierContactInfo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CurrencySymbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OrderType = table.Column<int>(type: "integer", nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    WHTRate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    WHTAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ShippingAddressId = table.Column<int>(type: "integer", nullable: true),
                    BillingAddressId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Addresses_BillingAddressId",
                        column: x => x.BillingAddressId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Addresses_ShippingAddressId",
                        column: x => x.ShippingAddressId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseOrderId = table.Column<int>(type: "integer", nullable: false),
                    ExternalOrderItemId = table.Column<int>(type: "integer", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CachedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExternallyModified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseOrderId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ObjectName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderFiles", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrderFiles_FileSize_Positive", "\"FileSize\" > 0");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderFiles_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_AddressType",
                table: "Addresses",
                column: "AddressType");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_AddressType_Country",
                table: "Addresses",
                columns: new[] { "AddressType", "Country" });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_City",
                table: "Addresses",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_Country",
                table: "Addresses",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_Country_City",
                table: "Addresses",
                columns: new[] { "Country", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_CreatedAt",
                table: "Addresses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_PostalCode",
                table: "Addresses",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityId",
                table: "AuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType",
                table: "AuditLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_Action",
                table: "AuditLogs",
                columns: new[] { "EntityType", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ExternalServiceName",
                table: "AuditLogs",
                column: "ExternalServiceName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ExternalServiceName_Timestamp",
                table: "AuditLogs",
                columns: new[] { "ExternalServiceName", "Timestamp" },
                filter: "\"ExternalServiceName\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp_Action",
                table: "AuditLogs",
                columns: new[] { "Timestamp", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserRole_Action_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserRole", "Action", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_AggregateId",
                table: "DomainEvents",
                column: "AggregateId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_AggregateType",
                table: "DomainEvents",
                column: "AggregateType");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_AggregateType_AggregateId",
                table: "DomainEvents",
                columns: new[] { "AggregateType", "AggregateId" });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_AggregateType_AggregateId_OccurredAt",
                table: "DomainEvents",
                columns: new[] { "AggregateType", "AggregateId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_CorrelationId",
                table: "DomainEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_CorrelationId_OccurredAt",
                table: "DomainEvents",
                columns: new[] { "CorrelationId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_EventType",
                table: "DomainEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_EventType_OccurredAt",
                table: "DomainEvents",
                columns: new[] { "EventType", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_IsProcessed",
                table: "DomainEvents",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_IsProcessed_OccurredAt",
                table: "DomainEvents",
                columns: new[] { "IsProcessed", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_IsProcessed_ProcessingAttempts",
                table: "DomainEvents",
                columns: new[] { "IsProcessed", "ProcessingAttempts" });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_OccurredAt",
                table: "DomainEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_Processing_Queue",
                table: "DomainEvents",
                columns: new[] { "IsProcessed", "ProcessingAttempts", "OccurredAt" },
                filter: "\"IsProcessed\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_UserId",
                table: "DomainEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_CachedAt",
                table: "OrderItems",
                column: "CachedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ExternallyModified",
                table: "OrderItems",
                column: "ExternallyModified");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ExternallyModified_CachedAt",
                table: "OrderItems",
                columns: new[] { "ExternallyModified", "CachedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ExternalOrderItemId",
                table: "OrderItems",
                column: "ExternalOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductCode",
                table: "OrderItems",
                column: "ProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_PurchaseOrderId",
                table: "OrderItems",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_PurchaseOrderId_ExternalOrderItemId_Unique",
                table: "OrderItems",
                columns: new[] { "PurchaseOrderId", "ExternalOrderItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_PurchaseOrderId_ProductCode",
                table: "OrderItems",
                columns: new[] { "PurchaseOrderId", "ProductCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderFiles_ContentType",
                table: "PurchaseOrderFiles",
                column: "ContentType");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderFiles_DocumentType",
                table: "PurchaseOrderFiles",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderFiles_IsDeleted",
                table: "PurchaseOrderFiles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderFiles_IsDeleted_PurchaseOrderId",
                table: "PurchaseOrderFiles",
                columns: new[] { "IsDeleted", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderFiles_ObjectName_Unique",
                table: "PurchaseOrderFiles",
                column: "ObjectName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderFiles_PurchaseOrderId",
                table: "PurchaseOrderFiles",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderFiles_PurchaseOrderId_DocumentType",
                table: "PurchaseOrderFiles",
                columns: new[] { "PurchaseOrderId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderFiles_UploadedAt",
                table: "PurchaseOrderFiles",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderFiles_UploadedAt_DocumentType",
                table: "PurchaseOrderFiles",
                columns: new[] { "UploadedAt", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderFiles_UploadedBy",
                table: "PurchaseOrderFiles",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_BillingAddressId",
                table: "PurchaseOrders",
                column: "BillingAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedAt",
                table: "PurchaseOrders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedAt_Status",
                table: "PurchaseOrders",
                columns: new[] { "CreatedAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_IsDeleted",
                table: "PurchaseOrders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderDate",
                table: "PurchaseOrders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderID",
                table: "PurchaseOrders",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNumber_Unique",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderType",
                table: "PurchaseOrders",
                column: "OrderType");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ShippingAddressId",
                table: "PurchaseOrders",
                column: "ShippingAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                table: "PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status_OrderType",
                table: "PurchaseOrders",
                columns: new[] { "Status", "OrderType" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierID",
                table: "PurchaseOrders",
                column: "SupplierID");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierID_Status",
                table: "PurchaseOrders",
                columns: new[] { "SupplierID", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DomainEvents");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "PurchaseOrderFiles");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "Addresses");
        }
    }
}
