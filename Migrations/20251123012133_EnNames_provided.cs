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
            // safe conditional drop — will not fail if table is missing
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.MusicMelody', N'U') IS NOT NULL
                    DROP TABLE dbo.MusicMelody;
            ");

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "Country",
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
 
        }
    }
}
