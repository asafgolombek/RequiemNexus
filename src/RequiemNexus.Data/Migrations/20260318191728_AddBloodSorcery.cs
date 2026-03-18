using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBloodSorcery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SorceryRiteDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    SorceryType = table.Column<int>(type: "integer", nullable: false),
                    XpCost = table.Column<int>(type: "integer", nullable: false),
                    PoolDefinitionJson = table.Column<string>(type: "text", maxLength: 2000, nullable: true),
                    ActivationCostDescription = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequiredCovenantId = table.Column<int>(type: "integer", nullable: false),
                    Prerequisites = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Effect = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SorceryRiteDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SorceryRiteDefinitions_CovenantDefinitions_RequiredCovenant~",
                        column: x => x.RequiredCovenantId,
                        principalTable: "CovenantDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterRites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    SorceryRiteDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StorytellerNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterRites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterRites_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterRites_SorceryRiteDefinitions_SorceryRiteDefinition~",
                        column: x => x.SorceryRiteDefinitionId,
                        principalTable: "SorceryRiteDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRites_CharacterId",
                table: "CharacterRites",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRites_SorceryRiteDefinitionId",
                table: "CharacterRites",
                column: "SorceryRiteDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SorceryRiteDefinitions_RequiredCovenantId",
                table: "SorceryRiteDefinitions",
                column: "RequiredCovenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterRites");

            migrationBuilder.DropTable(
                name: "SorceryRiteDefinitions");
        }
    }
}
