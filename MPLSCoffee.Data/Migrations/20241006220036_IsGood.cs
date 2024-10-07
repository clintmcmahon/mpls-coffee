using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MPLSCoffee.Data.Migrations
{
    /// <inheritdoc />
    public partial class IsGood : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGood",
                table: "CoffeeShops",
                type: "tinyint(1)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGood",
                table: "CoffeeShops");
        }
    }
}
