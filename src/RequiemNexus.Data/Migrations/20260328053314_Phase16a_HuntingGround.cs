using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase16a_HuntingGround : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PredatorType",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HuntingPoolDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PredatorType = table.Column<int>(type: "integer", nullable: false),
                    PoolDefinitionJson = table.Column<string>(type: "text", nullable: false),
                    BaseVitaeGain = table.Column<int>(type: "integer", nullable: false),
                    PerSuccessVitaeGain = table.Column<int>(type: "integer", nullable: false),
                    NarrativeDescription = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuntingPoolDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HuntingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    TerritoryId = table.Column<int>(type: "integer", nullable: true),
                    PredatorType = table.Column<int>(type: "integer", nullable: false),
                    PoolDescription = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Successes = table.Column<int>(type: "integer", nullable: false),
                    VitaeGained = table.Column<int>(type: "integer", nullable: false),
                    Resonance = table.Column<int>(type: "integer", nullable: false),
                    HuntedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuntingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HuntingRecords_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HuntingRecords_FeedingTerritories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "FeedingTerritories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HuntingPoolDefinitions_PredatorType",
                table: "HuntingPoolDefinitions",
                column: "PredatorType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HuntingRecords_CharacterId",
                table: "HuntingRecords",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_HuntingRecords_HuntedAt",
                table: "HuntingRecords",
                column: "HuntedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HuntingRecords_TerritoryId",
                table: "HuntingRecords",
                column: "TerritoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HuntingPoolDefinitions");

            migrationBuilder.DropTable(
                name: "HuntingRecords");

            migrationBuilder.DropColumn(
                name: "PredatorType",
                table: "Characters");
        }
    }
}
