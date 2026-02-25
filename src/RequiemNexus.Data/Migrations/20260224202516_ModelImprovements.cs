using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModelImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Willpower",
                table: "Characters",
                newName: "Size");

            migrationBuilder.RenameColumn(
                name: "Vitae",
                table: "Characters",
                newName: "MaxWillpower");

            migrationBuilder.RenameColumn(
                name: "Health",
                table: "Characters",
                newName: "MaxVitae");

            migrationBuilder.AddColumn<int>(
                name: "CurrentHealth",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentVitae",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentWillpower",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExperiencePoints",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Humanity",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxHealth",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Campaigns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Campaigns",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Campaigns",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_ApplicationUserId",
                table: "Campaigns",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_AspNetUsers_ApplicationUserId",
                table: "Campaigns",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_AspNetUsers_ApplicationUserId",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_ApplicationUserId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "CurrentHealth",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CurrentVitae",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CurrentWillpower",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ExperiencePoints",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Humanity",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "MaxHealth",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Campaigns");

            migrationBuilder.RenameColumn(
                name: "Size",
                table: "Characters",
                newName: "Willpower");

            migrationBuilder.RenameColumn(
                name: "MaxWillpower",
                table: "Characters",
                newName: "Vitae");

            migrationBuilder.RenameColumn(
                name: "MaxVitae",
                table: "Characters",
                newName: "Health");
        }
    }
}
