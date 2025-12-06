using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.PurchaseOrderService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureRowVersionGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pgcrypto extension for gen_random_bytes function
            migrationBuilder.Sql(@"CREATE EXTENSION IF NOT EXISTS pgcrypto;");

            // Set default value for RowVersion to use PostgreSQL's gen_random_bytes function
            migrationBuilder.Sql(@"
                ALTER TABLE ""PurchaseOrders""
                ALTER COLUMN ""RowVersion""
                SET DEFAULT gen_random_bytes(8);
            ");

            // Update existing rows with a default value
            migrationBuilder.Sql(@"
                UPDATE ""PurchaseOrders""
                SET ""RowVersion"" = gen_random_bytes(8)
                WHERE ""RowVersion"" IS NULL OR length(""RowVersion"") = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove default value for RowVersion
            migrationBuilder.Sql(@"
                ALTER TABLE ""PurchaseOrders""
                ALTER COLUMN ""RowVersion""
                DROP DEFAULT;
            ");
        }
    }
}
