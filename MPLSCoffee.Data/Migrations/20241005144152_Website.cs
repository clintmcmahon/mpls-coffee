using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MPLSCoffee.Data.Migrations
{
    /// <inheritdoc />
    public partial class Website : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "CoffeeShops",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Website",
                table: "CoffeeShops");
        }
    }
}
