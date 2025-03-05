using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Melodies25.Migrations
{
    /// <inheritdoc />
    public partial class Tonalities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tonality",
                table: "Melody",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tonality",
                table: "Melody");
        }
    }
}
