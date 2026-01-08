using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Melodies25.Migrations
{
    /// <inheritdoc />
    public partial class AddLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "Melody",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkToPDF",
                table: "Melody",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "Melody");

            migrationBuilder.DropColumn(
                name: "LinkToPDF",
                table: "Melody");
        }
    }
}
