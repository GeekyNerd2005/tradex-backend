using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tradex_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTypesAndEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExecutedAt",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ExecutedPrice",
                table: "Orders",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExecutedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExecutedPrice",
                table: "Orders");
        }
    }
}
