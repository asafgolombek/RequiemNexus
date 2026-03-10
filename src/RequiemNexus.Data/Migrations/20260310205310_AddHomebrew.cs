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
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHomebrew",
                table: "Merits",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HombrewAuthorUserId",
                table: "Disciplines",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHomebrew",
                table: "Disciplines",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HombrewAuthorUserId",
                table: "Clans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHomebrew",
                table: "Clans",
                type: "INTEGER",
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
