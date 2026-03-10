using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDanseMacabre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChronicleNpcs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CampaignId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PrimaryFactionId = table.Column<int>(type: "INTEGER", nullable: true),
                    RoleInFaction = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PublicDescription = table.Column<string>(type: "TEXT", nullable: false),
                    StorytellerNotes = table.Column<string>(type: "TEXT", nullable: false),
                    IsAlive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LinkedStatBlockId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicleNpcs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChronicleNpcs_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CityFactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CampaignId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FactionType = table.Column<int>(type: "INTEGER", nullable: false),
                    InfluenceRating = table.Column<int>(type: "INTEGER", nullable: false),
                    PublicDescription = table.Column<string>(type: "TEXT", nullable: false),
                    StorytellerNotes = table.Column<string>(type: "TEXT", nullable: false),
                    Agenda = table.Column<string>(type: "TEXT", nullable: false),
                    LeaderNpcId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityFactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CityFactions_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CityFactions_ChronicleNpcs_LeaderNpcId",
                        column: x => x.LeaderNpcId,
                        principalTable: "ChronicleNpcs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FactionRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CampaignId = table.Column<int>(type: "INTEGER", nullable: false),
                    FactionAId = table.Column<int>(type: "INTEGER", nullable: false),
                    FactionBId = table.Column<int>(type: "INTEGER", nullable: false),
                    StanceFromA = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactionRelationships_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FactionRelationships_CityFactions_FactionAId",
                        column: x => x.FactionAId,
                        principalTable: "CityFactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactionRelationships_CityFactions_FactionBId",
                        column: x => x.FactionBId,
                        principalTable: "CityFactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeedingTerritories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CampaignId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    ControlledByFactionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedingTerritories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedingTerritories_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedingTerritories_CityFactions_ControlledByFactionId",
                        column: x => x.ControlledByFactionId,
                        principalTable: "CityFactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleNpcs_CampaignId",
                table: "ChronicleNpcs",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleNpcs_PrimaryFactionId",
                table: "ChronicleNpcs",
                column: "PrimaryFactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CityFactions_CampaignId",
                table: "CityFactions",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CityFactions_LeaderNpcId",
                table: "CityFactions",
                column: "LeaderNpcId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelationships_CampaignId",
                table: "FactionRelationships",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelationships_FactionAId",
                table: "FactionRelationships",
                column: "FactionAId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelationships_FactionBId",
                table: "FactionRelationships",
                column: "FactionBId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedingTerritories_CampaignId",
                table: "FeedingTerritories",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedingTerritories_ControlledByFactionId",
                table: "FeedingTerritories",
                column: "ControlledByFactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChronicleNpcs_CityFactions_PrimaryFactionId",
                table: "ChronicleNpcs",
                column: "PrimaryFactionId",
                principalTable: "CityFactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChronicleNpcs_CityFactions_PrimaryFactionId",
                table: "ChronicleNpcs");

            migrationBuilder.DropTable(
                name: "FactionRelationships");

            migrationBuilder.DropTable(
                name: "FeedingTerritories");

            migrationBuilder.DropTable(
                name: "CityFactions");

            migrationBuilder.DropTable(
                name: "ChronicleNpcs");
        }
    }
}
