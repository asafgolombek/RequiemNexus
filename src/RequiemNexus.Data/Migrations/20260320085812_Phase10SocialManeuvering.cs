using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase10SocialManeuvering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SocialManeuvers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    InitiatorCharacterId = table.Column<int>(type: "integer", nullable: false),
                    TargetChronicleNpcId = table.Column<int>(type: "integer", nullable: false),
                    GoalDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    InitialDoors = table.Column<int>(type: "integer", nullable: false),
                    RemainingDoors = table.Column<int>(type: "integer", nullable: false),
                    CurrentImpression = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastRollAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CumulativePenaltyDice = table.Column<int>(type: "integer", nullable: false),
                    HostileSince = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialManeuvers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialManeuvers_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialManeuvers_Characters_InitiatorCharacterId",
                        column: x => x.InitiatorCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SocialManeuvers_ChronicleNpcs_TargetChronicleNpcId",
                        column: x => x.TargetChronicleNpcId,
                        principalTable: "ChronicleNpcs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManeuverClues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SocialManeuverId = table.Column<int>(type: "integer", nullable: false),
                    SourceDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsSpent = table.Column<bool>(type: "boolean", nullable: false),
                    Benefit = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    LeverageKind = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManeuverClues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManeuverClues_SocialManeuvers_SocialManeuverId",
                        column: x => x.SocialManeuverId,
                        principalTable: "SocialManeuvers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManeuverClues_SocialManeuverId",
                table: "ManeuverClues",
                column: "SocialManeuverId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialManeuvers_CampaignId",
                table: "SocialManeuvers",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialManeuvers_CampaignId_Status",
                table: "SocialManeuvers",
                columns: new[] { "CampaignId", "Status", });

            migrationBuilder.CreateIndex(
                name: "IX_SocialManeuvers_InitiatorCharacterId",
                table: "SocialManeuvers",
                column: "InitiatorCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialManeuvers_TargetChronicleNpcId",
                table: "SocialManeuvers",
                column: "TargetChronicleNpcId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManeuverClues");

            migrationBuilder.DropTable(
                name: "SocialManeuvers");
        }
    }
}
