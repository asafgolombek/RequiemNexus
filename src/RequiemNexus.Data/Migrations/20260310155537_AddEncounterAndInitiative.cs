using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEncounterAndInitiative : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CombatEncounters",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CampaignId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    ResolvedAt = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombatEncounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombatEncounters_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InitiativeEntries",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EncounterId = table.Column<int>(nullable: false),
                    CharacterId = table.Column<int>(nullable: true),
                    NpcName = table.Column<string>(maxLength: 200, nullable: true),
                    InitiativeMod = table.Column<int>(nullable: false),
                    RollResult = table.Column<int>(nullable: false),
                    Total = table.Column<int>(nullable: false),
                    HasActed = table.Column<bool>(nullable: false),
                    Order = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiativeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InitiativeEntries_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InitiativeEntries_CombatEncounters_EncounterId",
                        column: x => x.EncounterId,
                        principalTable: "CombatEncounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CombatEncounters_CampaignId",
                table: "CombatEncounters",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeEntries_CharacterId",
                table: "InitiativeEntries",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeEntries_EncounterId",
                table: "InitiativeEntries",
                column: "EncounterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InitiativeEntries");

            migrationBuilder.DropTable(
                name: "CombatEncounters");
        }
    }
}
