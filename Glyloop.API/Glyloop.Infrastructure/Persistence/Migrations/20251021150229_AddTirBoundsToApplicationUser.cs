using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glyloop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTirBoundsToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TirLowerBound",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TirUpperBound",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TirLowerBound",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TirUpperBound",
                table: "AspNetUsers");
        }
    }
}
