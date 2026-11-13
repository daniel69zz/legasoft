using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Api.Migrations
{
    /// <inheritdoc />
    public partial class EntregaDeCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "handover_confirmed_at",
                schema: "cash",
                table: "sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "handover_received_amount",
                schema: "cash",
                table: "sessions",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "handover_to",
                schema: "cash",
                table: "sessions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "handover_confirmed_at",
                schema: "cash",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "handover_received_amount",
                schema: "cash",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "handover_to",
                schema: "cash",
                table: "sessions");
        }
    }
}
