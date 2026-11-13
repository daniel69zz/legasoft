using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Business.Api.Migrations
{
    /// <inheritdoc />
    public partial class ComprasYRecepcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lot_feed",
                schema: "inventory",
                columns: table => new
                {
                    sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_number = table.Column<string>(type: "text", nullable: false),
                    expiration_date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lot_feed", x => x.sequence);
                });

            migrationBuilder.CreateTable(
                name: "purchases",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "text", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_name = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    received_by = table.Column<string>(type: "text", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_lines",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    received_quantity = table.Column<int>(type: "integer", nullable: false),
                    damaged_quantity = table.Column<int>(type: "integer", nullable: false),
                    lot_number = table.Column<string>(type: "text", nullable: false),
                    expiration_date = table.Column<DateOnly>(type: "date", nullable: true),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_purchase_lines_purchases_purchase_id",
                        column: x => x.purchase_id,
                        principalSchema: "inventory",
                        principalTable: "purchases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_lines_product_id",
                schema: "inventory",
                table: "purchase_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_lines_purchase_id",
                schema: "inventory",
                table: "purchase_lines",
                column: "purchase_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchases_number",
                schema: "inventory",
                table: "purchases",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchases_supplier_id",
                schema: "inventory",
                table: "purchases",
                column: "supplier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lot_feed",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "purchase_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "purchases",
                schema: "inventory");
        }
    }
}
