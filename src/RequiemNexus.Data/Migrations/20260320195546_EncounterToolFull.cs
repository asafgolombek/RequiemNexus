using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class EncounterToolFull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHeld",
                table: "InitiativeEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRevealed",
                table: "InitiativeEntries",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "MaskedDisplayName",
                table: "InitiativeEntries",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NpcHealthBoxes",
                table: "InitiativeEntries",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<string>(
                name: "NpcHealthDamage",
                table: "InitiativeEntries",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<int>(
                name: "CurrentRound",
                table: "CombatEncounters",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "CombatEncounters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EncounterNpcTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EncounterId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InitiativeMod = table.Column<int>(type: "integer", nullable: false),
                    HealthBoxes = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsRevealed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DefaultMaskedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncounterNpcTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EncounterNpcTemplates_CombatEncounters_EncounterId",
                        column: x => x.EncounterId,
                        principalTable: "CombatEncounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EncounterTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncounterTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EncounterTemplates_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EncounterTemplateNpcs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InitiativeMod = table.Column<int>(type: "integer", nullable: false),
                    HealthBoxes = table.Column<int>(type: "integer", nullable: false),
                    IsRevealedByDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DefaultMaskedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncounterTemplateNpcs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EncounterTemplateNpcs_EncounterTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "EncounterTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EncounterNpcTemplates_EncounterId",
                table: "EncounterNpcTemplates",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_EncounterTemplateNpcs_TemplateId",
                table: "EncounterTemplateNpcs",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EncounterTemplates_CampaignId",
                table: "EncounterTemplates",
                column: "CampaignId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EncounterNpcTemplates");

            migrationBuilder.DropTable(
                name: "EncounterTemplateNpcs");

            migrationBuilder.DropTable(
                name: "EncounterTemplates");

            migrationBuilder.DropColumn(
                name: "IsHeld",
                table: "InitiativeEntries");

            migrationBuilder.DropColumn(
                name: "IsRevealed",
                table: "InitiativeEntries");

            migrationBuilder.DropColumn(
                name: "MaskedDisplayName",
                table: "InitiativeEntries");

            migrationBuilder.DropColumn(
                name: "NpcHealthBoxes",
                table: "InitiativeEntries");

            migrationBuilder.DropColumn(
                name: "NpcHealthDamage",
                table: "InitiativeEntries");

            migrationBuilder.DropColumn(
                name: "CurrentRound",
                table: "CombatEncounters");

            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "CombatEncounters");
        }
    }
}
