using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterIdentityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Backstory",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Concept",
                table: "Characters",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EyeColor",
                table: "Characters",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HairColor",
                table: "Characters",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Height",
                table: "Characters",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalExperiencePoints",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Backstory",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Concept",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "EyeColor",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "HairColor",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "TotalExperiencePoints",
                table: "Characters");
        }
    }
}
