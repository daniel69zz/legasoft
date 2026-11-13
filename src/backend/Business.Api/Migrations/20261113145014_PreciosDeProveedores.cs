using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Api.Migrations
{
    /// <inheritdoc />
    public partial class PreciosDeProveedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_prices",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    registered_by = table.Column<string>(type: "text", nullable: false),
                    registered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_prices", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_prices_product_id",
                schema: "inventory",
                table: "supplier_prices",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_prices_supplier_id_product_id",
                schema: "inventory",
                table: "supplier_prices",
                columns: new[] { "supplier_id", "product_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_prices",
                schema: "inventory");
        }
    }
}
