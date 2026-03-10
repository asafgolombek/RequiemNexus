using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBeatAndXpLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BeatLedger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    CampaignId = table.Column<int>(type: "INTEGER", nullable: true),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AwardedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatLedger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeatLedger_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BeatLedger_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "XpLedger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    CampaignId = table.Column<int>(type: "INTEGER", nullable: true),
                    Delta = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: true),
                    Expense = table.Column<int>(type: "INTEGER", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActingUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XpLedger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XpLedger_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XpLedger_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeatLedger_CampaignId",
                table: "BeatLedger",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatLedger_CharacterId",
                table: "BeatLedger",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatLedger_OccurredAt",
                table: "BeatLedger",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_XpLedger_CampaignId",
                table: "XpLedger",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_XpLedger_CharacterId",
                table: "XpLedger",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_XpLedger_OccurredAt",
                table: "XpLedger",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeatLedger");

            migrationBuilder.DropTable(
                name: "XpLedger");
        }
    }
}
