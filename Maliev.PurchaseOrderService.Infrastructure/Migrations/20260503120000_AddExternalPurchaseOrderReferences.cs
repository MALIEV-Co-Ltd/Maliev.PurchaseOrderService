using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.PurchaseOrderService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalPurchaseOrderReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyServiceId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceOrderId",
                table: "PurchaseOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierServiceId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceOrderItemId",
                table: "OrderItems",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyServiceId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SourceOrderId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SupplierServiceId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SourceOrderItemId",
                table: "OrderItems");
        }
    }
}
