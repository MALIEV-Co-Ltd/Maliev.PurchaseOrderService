using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.PurchaseOrderService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModernizePurchaseOrderService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix__purchase_orders__order_number",
                table: "purchase_orders");

            migrationBuilder.AddColumn<int>(
                name: "department_id",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "purchase_orders",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "ix__purchase_orders__order_number",
                table: "purchase_orders",
                column: "order_number",
                unique: true,
                filter: "\"is_deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix__purchase_orders__order_number",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "department_id",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "purchase_orders");

            migrationBuilder.CreateIndex(
                name: "ix__purchase_orders__order_number",
                table: "purchase_orders",
                column: "order_number",
                unique: true);
        }
    }
}
