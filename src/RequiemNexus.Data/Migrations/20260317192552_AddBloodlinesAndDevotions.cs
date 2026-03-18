using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBloodlinesAndDevotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BloodlineDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    FourthDisciplineId = table.Column<int>(type: "integer", nullable: false),
                    PrerequisiteBloodPotency = table.Column<int>(type: "integer", nullable: false),
                    BaneOverride = table.Column<string>(type: "text", nullable: false),
                    CustomRuleOverride = table.Column<bool>(type: "boolean", nullable: false),
                    CustomRuleOverrideDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloodlineDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloodlineDefinitions_Disciplines_FourthDisciplineId",
                        column: x => x.FourthDisciplineId,
                        principalTable: "Disciplines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BloodlineClans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BloodlineDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    ClanId = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloodlineClans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloodlineClans_BloodlineDefinitions_BloodlineDefinitionId",
                        column: x => x.BloodlineDefinitionId,
                        principalTable: "BloodlineDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BloodlineClans_Clans_ClanId",
                        column: x => x.ClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterBloodlines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    BloodlineDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StorytellerNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterBloodlines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterBloodlines_BloodlineDefinitions_BloodlineDefinitio~",
                        column: x => x.BloodlineDefinitionId,
                        principalTable: "BloodlineDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharacterBloodlines_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BloodlineClans_BloodlineDefinitionId",
                table: "BloodlineClans",
                column: "BloodlineDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_BloodlineClans_ClanId",
                table: "BloodlineClans",
                column: "ClanId");

            migrationBuilder.CreateIndex(
                name: "IX_BloodlineDefinitions_FourthDisciplineId",
                table: "BloodlineDefinitions",
                column: "FourthDisciplineId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterBloodlines_BloodlineDefinitionId",
                table: "CharacterBloodlines",
                column: "BloodlineDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterBloodlines_CharacterId",
                table: "CharacterBloodlines",
                column: "CharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloodlineClans");

            migrationBuilder.DropTable(
                name: "CharacterBloodlines");

            migrationBuilder.DropTable(
                name: "BloodlineDefinitions");
        }
    }
}
