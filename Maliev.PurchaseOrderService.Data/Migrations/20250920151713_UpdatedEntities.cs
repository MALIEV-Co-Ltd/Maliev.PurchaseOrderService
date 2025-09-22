using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.PurchaseOrderService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastModifiedBy",
                table: "PurchaseOrders",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAt",
                table: "PurchaseOrders",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAt",
                table: "Addresses",
                newName: "ValidatedAt");

            migrationBuilder.AddColumn<int>(
                name: "GeneratedPdfFileId",
                table: "PurchaseOrders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPdfGenerated",
                table: "PurchaseOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPdfGenerationEnabled",
                table: "PurchaseOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PdfGeneratedAt",
                table: "PurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfGeneratedBy",
                table: "PurchaseOrders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DownloadCount",
                table: "PurchaseOrderFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExternalUrl",
                table: "PurchaseOrderFiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExternalUrlExpiration",
                table: "PurchaseOrderFiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "PurchaseOrderFiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "PurchaseOrderFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemGenerated",
                table: "PurchaseOrderFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDownloadedAt",
                table: "PurchaseOrderFiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastDownloadedBy",
                table: "PurchaseOrderFiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PurchaseOrderFiles",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PurchaseOrderFiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "PurchaseOrderFiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VirusScanCompletedAt",
                table: "PurchaseOrderFiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VirusScanStatus",
                table: "PurchaseOrderFiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "OrderItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ExternalStatus",
                table: "OrderItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalVersion",
                table: "OrderItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSyncSuccessful",
                table: "OrderItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncError",
                table: "OrderItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceService",
                table: "OrderItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "OrderItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventHeaders",
                table: "DomainEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventSource",
                table: "DomainEvents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "DomainEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRetryAttempts",
                table: "DomainEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "DomainEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartitionKey",
                table: "DomainEvents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "DomainEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedByHandler",
                table: "DomainEvents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingResult",
                table: "DomainEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationVersion",
                table: "AuditLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Environment",
                table: "AuditLogs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "AuditLogs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccessful",
                table: "AuditLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OperationDurationMs",
                table: "AuditLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestId",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Addresses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Addresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Addresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsValidated",
                table: "Addresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Addresses",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Addresses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Addresses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_GeneratedPdfFileId",
                table: "PurchaseOrders",
                column: "GeneratedPdfFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_PurchaseOrderFiles_GeneratedPdfFileId",
                table: "PurchaseOrders",
                column: "GeneratedPdfFileId",
                principalTable: "PurchaseOrderFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_PurchaseOrderFiles_GeneratedPdfFileId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_GeneratedPdfFileId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "GeneratedPdfFileId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "IsPdfGenerated",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "IsPdfGenerationEnabled",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "PdfGeneratedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "PdfGeneratedBy",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DownloadCount",
                table: "PurchaseOrderFiles");

            migrationBuilder.DropColumn(
                name: "ExternalUrl",
                table: "PurchaseOrderFiles");

            migrationBuilder.DropColumn(
                name: "ExternalUrlExpiration",
                table: "PurchaseOrderFiles");

            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "PurchaseOrderFiles");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "PurchaseOrderFiles");

            migrationBuilder.DropColumn(
                name: "IsSystemGenerated",
                table: "PurchaseOrderFiles");

            migrationBuilder.DropColumn(
                name: "LastDownloadedAt",
                table: "PurchaseOrderFiles");

            migrationBuilder.DropColumn(
                name: "LastDownloadedBy",
                table: "PurchaseOrderFiles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PurchaseOrderFiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PurchaseOrderFiles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PurchaseOrderFiles");

            migrationBuilder.DropColumn(
                name: "VirusScanCompletedAt",
                table: "PurchaseOrderFiles");

            migrationBuilder.DropColumn(
                name: "VirusScanStatus",
                table: "PurchaseOrderFiles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ExternalStatus",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ExternalVersion",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "IsSyncSuccessful",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SourceService",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "EventHeaders",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "EventSource",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "MaxRetryAttempts",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "PartitionKey",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "ProcessedByHandler",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "ProcessingResult",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "ApplicationVersion",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Environment",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IsSuccessful",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "OperationDurationMs",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "IsValidated",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Addresses");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "PurchaseOrders",
                newName: "LastModifiedBy");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "PurchaseOrders",
                newName: "LastModifiedAt");

            migrationBuilder.RenameColumn(
                name: "ValidatedAt",
                table: "Addresses",
                newName: "LastModifiedAt");
        }
    }
}
