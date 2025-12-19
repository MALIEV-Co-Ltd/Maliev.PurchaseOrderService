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
                name: "addresses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    address_type = table.Column<int>(type: "integer", nullable: false),
                    company_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    contact_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_line1 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_line2 = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    state_province = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_addresses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    old_values = table.Column<string>(type: "text", nullable: true),
                    new_values = table.Column<string>(type: "text", nullable: true),
                    external_service_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    external_service_response = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    change_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "domain_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    aggregate_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_data = table.Column<string>(type: "text", nullable: false),
                    event_version = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_processed = table.Column<bool>(type: "boolean", nullable: false),
                    processing_attempts = table.Column<int>(type: "integer", nullable: false),
                    last_processing_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_domain_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    customer_po = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    supplier_contact_info = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    currency_id = table.Column<int>(type: "integer", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    currency_symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    order_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expected_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    order_type = table.Column<int>(type: "integer", nullable: false),
                    subtotal_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    wht_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    wht_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    shipping_address_id = table.Column<int>(type: "integer", nullable: true),
                    billing_address_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_orders_addresses_billing_address_id",
                        column: x => x.billing_address_id,
                        principalTable: "addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_orders_addresses_shipping_address_id",
                        column: x => x.shipping_address_id,
                        principalTable: "addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    external_order_item_id = table.Column<int>(type: "integer", nullable: false),
                    product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cached_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    externally_modified = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_items__purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_files",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    object_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    document_type = table.Column<int>(type: "integer", nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    uploaded_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_order_files_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix__audit_log__entity_type__entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix__audit_log__timestamp",
                table: "audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix__audit_log__user_id__timestamp",
                table: "audit_logs",
                columns: new[] { "user_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix__domain_events__correlation_id",
                table: "domain_events",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix__domain_events__event_type__aggregate_id",
                table: "domain_events",
                columns: new[] { "event_type", "aggregate_id" });

            migrationBuilder.CreateIndex(
                name: "ix__domain_events__is_processed__occurred_at",
                table: "domain_events",
                columns: new[] { "is_processed", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix__domain_events__processing_attempts",
                table: "domain_events",
                column: "processing_attempts");

            migrationBuilder.CreateIndex(
                name: "ix__order_items__product_code",
                table: "order_items",
                column: "product_code");

            migrationBuilder.CreateIndex(
                name: "ix__order_items__purchase_order_id",
                table: "order_items",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix__purchase_order_files__document_type",
                table: "purchase_order_files",
                column: "document_type");

            migrationBuilder.CreateIndex(
                name: "ix__purchase_order_files__object_name",
                table: "purchase_order_files",
                column: "object_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix__purchase_order_files__purchase_order_id",
                table: "purchase_order_files",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix__purchase_order_files__uploaded_by",
                table: "purchase_order_files",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_billing_address_id",
                table: "purchase_orders",
                column: "billing_address_id");

            migrationBuilder.CreateIndex(
                name: "ix__purchase_orders__created_at",
                table: "purchase_orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix__purchase_orders__created_by__status",
                table: "purchase_orders",
                columns: new[] { "created_by", "status" });

            migrationBuilder.CreateIndex(
                name: "ix__purchase_orders__order_id",
                table: "purchase_orders",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix__purchase_orders__order_number",
                table: "purchase_orders",
                column: "order_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_shipping_address_id",
                table: "purchase_orders",
                column: "shipping_address_id");

            migrationBuilder.CreateIndex(
                name: "ix__purchase_orders__status__order_type",
                table: "purchase_orders",
                columns: new[] { "status", "order_type" });

            migrationBuilder.CreateIndex(
                name: "ix__purchase_orders__supplier_id",
                table: "purchase_orders",
                column: "supplier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "domain_events");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "purchase_order_files");

            migrationBuilder.DropTable(
                name: "purchase_orders");

            migrationBuilder.DropTable(
                name: "addresses");
        }
    }
}
