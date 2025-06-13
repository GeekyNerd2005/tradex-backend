using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tradex_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLastPriceToHoldings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "TotalValue",
                table: "PortfolioSnapshots",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<double>(
                name: "LastPrice",
                table: "Holdings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPrice",
                table: "Holdings");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalValue",
                table: "PortfolioSnapshots",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");
        }
    }
}
