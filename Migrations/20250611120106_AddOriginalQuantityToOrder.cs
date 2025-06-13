using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tradex_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddOriginalQuantityToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "OriginalQuantity",
                table: "Orders",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalQuantity",
                table: "Orders");
        }
    }
}
