using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class NpcWillpowerTracks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NpcCurrentWillpower",
                table: "InitiativeEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NpcMaxWillpower",
                table: "InitiativeEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxWillpower",
                table: "EncounterTemplateNpcs",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<int>(
                name: "MaxWillpower",
                table: "EncounterNpcTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.Sql(
                """
                UPDATE "InitiativeEntries" SET "NpcMaxWillpower" = 4, "NpcCurrentWillpower" = 4
                WHERE "CharacterId" IS NULL AND "NpcName" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NpcCurrentWillpower",
                table: "InitiativeEntries");

            migrationBuilder.DropColumn(
                name: "NpcMaxWillpower",
                table: "InitiativeEntries");

            migrationBuilder.DropColumn(
                name: "MaxWillpower",
                table: "EncounterTemplateNpcs");

            migrationBuilder.DropColumn(
                name: "MaxWillpower",
                table: "EncounterNpcTemplates");
        }
    }
}
