using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncBloodlinesDevotionsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DevotionDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    XpCost = table.Column<int>(type: "integer", nullable: false),
                    PoolDefinitionJson = table.Column<string>(type: "text", maxLength: 2000, nullable: true),
                    IsPassive = table.Column<bool>(type: "boolean", nullable: false),
                    ActivationCostDescription = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RequiredBloodlineId = table.Column<int>(type: "integer", nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevotionDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevotionDefinitions_BloodlineDefinitions_RequiredBloodlineId",
                        column: x => x.RequiredBloodlineId,
                        principalTable: "BloodlineDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CharacterDevotions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    DevotionDefinitionId = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterDevotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterDevotions_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterDevotions_DevotionDefinitions_DevotionDefinitionId",
                        column: x => x.DevotionDefinitionId,
                        principalTable: "DevotionDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DevotionPrerequisites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DevotionDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    DisciplineId = table.Column<int>(type: "integer", nullable: false),
                    MinimumLevel = table.Column<int>(type: "integer", nullable: false),
                    OrGroupId = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevotionPrerequisites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevotionPrerequisites_DevotionDefinitions_DevotionDefinitio~",
                        column: x => x.DevotionDefinitionId,
                        principalTable: "DevotionDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DevotionPrerequisites_Disciplines_DisciplineId",
                        column: x => x.DisciplineId,
                        principalTable: "Disciplines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterDevotions_CharacterId",
                table: "CharacterDevotions",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterDevotions_DevotionDefinitionId",
                table: "CharacterDevotions",
                column: "DevotionDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DevotionDefinitions_RequiredBloodlineId",
                table: "DevotionDefinitions",
                column: "RequiredBloodlineId");

            migrationBuilder.CreateIndex(
                name: "IX_DevotionPrerequisites_DevotionDefinitionId",
                table: "DevotionPrerequisites",
                column: "DevotionDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DevotionPrerequisites_DisciplineId",
                table: "DevotionPrerequisites",
                column: "DisciplineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterDevotions");

            migrationBuilder.DropTable(
                name: "DevotionPrerequisites");

            migrationBuilder.DropTable(
                name: "DevotionDefinitions");
        }
    }
}
