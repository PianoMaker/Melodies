using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Melodies25.Migrations
{
    /// <inheritdoc />
    public partial class EnNames_provided : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "Country",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitleEn",
                table: "Melody",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "Country");

            migrationBuilder.DropColumn(
                    name: "TitleEn",
                    table: "Melody");

        }
    }
}
