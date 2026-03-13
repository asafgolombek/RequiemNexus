#pragma warning disable S1192 // String literals should not be duplicated
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
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Concept",
                table: "Characters",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EyeColor",
                table: "Characters",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HairColor",
                table: "Characters",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Height",
                table: "Characters",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalExperiencePoints",
                table: "Characters",
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

