using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHomebrew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HombrewAuthorUserId",
                table: "Merits",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHomebrew",
                table: "Merits",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HombrewAuthorUserId",
                table: "Disciplines",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHomebrew",
                table: "Disciplines",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HombrewAuthorUserId",
                table: "Clans",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHomebrew",
                table: "Clans",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HombrewAuthorUserId",
                table: "Merits");

            migrationBuilder.DropColumn(
                name: "IsHomebrew",
                table: "Merits");

            migrationBuilder.DropColumn(
                name: "HombrewAuthorUserId",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "IsHomebrew",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "HombrewAuthorUserId",
                table: "Clans");

            migrationBuilder.DropColumn(
                name: "IsHomebrew",
                table: "Clans");
        }
    }
}
