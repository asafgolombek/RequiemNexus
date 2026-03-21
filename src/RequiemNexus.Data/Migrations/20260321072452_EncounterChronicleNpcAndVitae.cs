using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class EncounterChronicleNpcAndVitae : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChronicleNpcId",
                table: "InitiativeEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NpcCurrentVitae",
                table: "InitiativeEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NpcMaxVitae",
                table: "InitiativeEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChronicleNpcId",
                table: "EncounterNpcTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxVitae",
                table: "EncounterNpcTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeEntries_ChronicleNpcId",
                table: "InitiativeEntries",
                column: "ChronicleNpcId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeEntries_EncounterId_ChronicleNpcId",
                table: "InitiativeEntries",
                columns: new[] { "EncounterId", "ChronicleNpcId" },
                unique: true,
                filter: "\"ChronicleNpcId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EncounterNpcTemplates_ChronicleNpcId",
                table: "EncounterNpcTemplates",
                column: "ChronicleNpcId");

            migrationBuilder.CreateIndex(
                name: "IX_EncounterNpcTemplates_EncounterId_ChronicleNpcId",
                table: "EncounterNpcTemplates",
                columns: new[] { "EncounterId", "ChronicleNpcId" },
                unique: true,
                filter: "\"ChronicleNpcId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_EncounterNpcTemplates_ChronicleNpcs_ChronicleNpcId",
                table: "EncounterNpcTemplates",
                column: "ChronicleNpcId",
                principalTable: "ChronicleNpcs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InitiativeEntries_ChronicleNpcs_ChronicleNpcId",
                table: "InitiativeEntries",
                column: "ChronicleNpcId",
                principalTable: "ChronicleNpcs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EncounterNpcTemplates_ChronicleNpcs_ChronicleNpcId",
                table: "EncounterNpcTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_InitiativeEntries_ChronicleNpcs_ChronicleNpcId",
                table: "InitiativeEntries");

            migrationBuilder.DropIndex(
                name: "IX_InitiativeEntries_ChronicleNpcId",
                table: "InitiativeEntries");

            migrationBuilder.DropIndex(
                name: "IX_InitiativeEntries_EncounterId_ChronicleNpcId",
                table: "InitiativeEntries");

            migrationBuilder.DropIndex(
                name: "IX_EncounterNpcTemplates_ChronicleNpcId",
                table: "EncounterNpcTemplates");

            migrationBuilder.DropIndex(
                name: "IX_EncounterNpcTemplates_EncounterId_ChronicleNpcId",
                table: "EncounterNpcTemplates");

            migrationBuilder.DropColumn(
                name: "ChronicleNpcId",
                table: "InitiativeEntries");

            migrationBuilder.DropColumn(
                name: "NpcCurrentVitae",
                table: "InitiativeEntries");

            migrationBuilder.DropColumn(
                name: "NpcMaxVitae",
                table: "InitiativeEntries");

            migrationBuilder.DropColumn(
                name: "ChronicleNpcId",
                table: "EncounterNpcTemplates");

            migrationBuilder.DropColumn(
                name: "MaxVitae",
                table: "EncounterNpcTemplates");
        }
    }
}
