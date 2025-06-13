using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tradex_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRealizedPnLToTrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "RealizedPnL",
                table: "Trades",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RealizedPnL",
                table: "Trades");
        }
    }
}
