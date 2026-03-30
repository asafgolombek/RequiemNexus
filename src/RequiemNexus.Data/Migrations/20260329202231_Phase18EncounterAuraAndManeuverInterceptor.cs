using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase18EncounterAuraAndManeuverInterceptor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EncounterAuraContests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EncounterId = table.Column<int>(type: "integer", nullable: false),
                    VampireLowerId = table.Column<int>(type: "integer", nullable: false),
                    VampireHigherId = table.Column<int>(type: "integer", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncounterAuraContests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EncounterAuraContests_Characters_VampireHigherId",
                        column: x => x.VampireHigherId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EncounterAuraContests_Characters_VampireLowerId",
                        column: x => x.VampireLowerId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EncounterAuraContests_CombatEncounters_EncounterId",
                        column: x => x.EncounterId,
                        principalTable: "CombatEncounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManeuverInterceptors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SocialManeuverId = table.Column<int>(type: "integer", nullable: false),
                    InterceptorCharacterId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Successes = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManeuverInterceptors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManeuverInterceptors_Characters_InterceptorCharacterId",
                        column: x => x.InterceptorCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManeuverInterceptors_SocialManeuvers_SocialManeuverId",
                        column: x => x.SocialManeuverId,
                        principalTable: "SocialManeuvers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EncounterAuraContests_EncounterId_VampireLowerId_VampireHig~",
                table: "EncounterAuraContests",
                columns: new[] { "EncounterId", "VampireLowerId", "VampireHigherId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EncounterAuraContests_VampireHigherId",
                table: "EncounterAuraContests",
                column: "VampireHigherId");

            migrationBuilder.CreateIndex(
                name: "IX_EncounterAuraContests_VampireLowerId",
                table: "EncounterAuraContests",
                column: "VampireLowerId");

            migrationBuilder.CreateIndex(
                name: "IX_ManeuverInterceptors_InterceptorCharacterId",
                table: "ManeuverInterceptors",
                column: "InterceptorCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_ManeuverInterceptors_SocialManeuverId_InterceptorCharacterId",
                table: "ManeuverInterceptors",
                columns: new[] { "SocialManeuverId", "InterceptorCharacterId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EncounterAuraContests");

            migrationBuilder.DropTable(
                name: "ManeuverInterceptors");
        }
    }
}
