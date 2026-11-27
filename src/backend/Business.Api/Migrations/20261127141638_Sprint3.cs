using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "branch",
                schema: "inventory",
                table: "stock_movements",
                type: "text",
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "branch",
                schema: "cash",
                table: "sessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "branch",
                schema: "sales",
                table: "sales",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "discount_authorized_by",
                schema: "sales",
                table: "sales",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "prescription_doctor",
                schema: "sales",
                table: "sales",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "prescription_issued_on",
                schema: "sales",
                table: "sales",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prescription_patient",
                schema: "sales",
                table: "sales",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "branch",
                schema: "cash",
                table: "movements",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "arqueos",
                schema: "cash",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cash_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<string>(type: "text", nullable: false),
                    user_name = table.Column<string>(type: "text", nullable: false),
                    expected_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    counted_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    difference = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_arqueos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    tax_id = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_number = table.Column<string>(type: "text", nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    issued_by = table.Column<string>(type: "text", nullable: false),
                    customer_name = table.Column<string>(type: "text", nullable: false),
                    customer_tax_id = table.Column<string>(type: "text", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_arqueos_cash_session_id",
                schema: "cash",
                table: "arqueos",
                column: "cash_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_name",
                schema: "sales",
                table: "customers",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_customers_tax_id",
                schema: "sales",
                table: "customers",
                column: "tax_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_number",
                schema: "sales",
                table: "invoices",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_sale_id",
                schema: "sales",
                table: "invoices",
                column: "sale_id",
                unique: true);

            // SCRUM-34 · HU-29: los datos anteriores al Sprint 3 pertenecen a la sucursal original.
            migrationBuilder.Sql("UPDATE sales.sales SET branch = 'Sopocachi' WHERE branch = '';");
            migrationBuilder.Sql("UPDATE cash.sessions SET branch = 'Sopocachi' WHERE branch = '';");
            migrationBuilder.Sql("UPDATE cash.movements SET branch = 'Sopocachi' WHERE branch = '';");
            migrationBuilder.Sql("UPDATE inventory.stock_movements SET branch = 'Sopocachi' WHERE branch = '';");

            // SCRUM-13 · HU-20 (RF-075): la bitácora solo admite altas. Un UPDATE, DELETE o TRUNCATE se rechaza en
            // la propia base; solo la recuperación de un respaldo (SCRUM-36) desactiva el trigger dentro de su transacción.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION audit.reject_change() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'La bitácora de auditoría no se puede modificar ni borrar (RF-075)';
                END $$;
                CREATE TRIGGER entries_append_only BEFORE UPDATE OR DELETE ON audit.entries
                    FOR EACH ROW EXECUTE FUNCTION audit.reject_change();
                CREATE TRIGGER entries_no_truncate BEFORE TRUNCATE ON audit.entries
                    FOR EACH STATEMENT EXECUTE FUNCTION audit.reject_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS entries_append_only ON audit.entries;
                DROP TRIGGER IF EXISTS entries_no_truncate ON audit.entries;
                DROP FUNCTION IF EXISTS audit.reject_change();
                """);

            migrationBuilder.DropTable(
                name: "arqueos",
                schema: "cash");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "sales");

            migrationBuilder.DropColumn(
                name: "branch",
                schema: "inventory",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "branch",
                schema: "cash",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "branch",
                schema: "sales",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "discount_authorized_by",
                schema: "sales",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "prescription_doctor",
                schema: "sales",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "prescription_issued_on",
                schema: "sales",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "prescription_patient",
                schema: "sales",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "branch",
                schema: "cash",
                table: "movements");
        }
    }
}
