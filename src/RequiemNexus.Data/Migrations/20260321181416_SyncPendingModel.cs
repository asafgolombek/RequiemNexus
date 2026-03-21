using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InitiativeEntries_EncounterId_ChronicleNpcId",
                table: "InitiativeEntries");

            migrationBuilder.DropIndex(
                name: "IX_EncounterNpcTemplates_EncounterId_ChronicleNpcId",
                table: "EncounterNpcTemplates");

            migrationBuilder.AddColumn<string>(
                name: "MaskedDisplayName",
                table: "EncounterNpcTemplates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeEntries_EncounterId_ChronicleNpcId",
                table: "InitiativeEntries",
                columns: new[] { "EncounterId", "ChronicleNpcId" },
                unique: true,
                filter: "\"ChronicleNpcId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EncounterNpcTemplates_EncounterId_ChronicleNpcId",
                table: "EncounterNpcTemplates",
                columns: new[] { "EncounterId", "ChronicleNpcId" },
                unique: true,
                filter: "\"ChronicleNpcId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InitiativeEntries_EncounterId_ChronicleNpcId",
                table: "InitiativeEntries");

            migrationBuilder.DropIndex(
                name: "IX_EncounterNpcTemplates_EncounterId_ChronicleNpcId",
                table: "EncounterNpcTemplates");

            migrationBuilder.DropColumn(
                name: "MaskedDisplayName",
                table: "EncounterNpcTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeEntries_EncounterId_ChronicleNpcId",
                table: "InitiativeEntries",
                columns: new[] { "EncounterId", "ChronicleNpcId" },
                unique: true,
                filter: "\"ChronicleNpcId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EncounterNpcTemplates_EncounterId_ChronicleNpcId",
                table: "EncounterNpcTemplates",
                columns: new[] { "EncounterId", "ChronicleNpcId" },
                unique: true,
                filter: "\"ChronicleNpcId\" IS NOT NULL");
        }
    }
}
