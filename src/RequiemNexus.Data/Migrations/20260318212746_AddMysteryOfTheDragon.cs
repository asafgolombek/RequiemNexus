using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMysteryOfTheDragon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChosenMysteryScaleId",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasCrucibleRitualAccess",
                table: "Characters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PendingChosenMysteryScaleId",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScaleDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    MysteryName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MaxLevel = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScaleDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CoilDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ScaleId = table.Column<int>(type: "integer", nullable: false),
                    PrerequisiteCoilId = table.Column<int>(type: "integer", nullable: true),
                    RollDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ModifiersJson = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoilDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoilDefinitions_CoilDefinitions_PrerequisiteCoilId",
                        column: x => x.PrerequisiteCoilId,
                        principalTable: "CoilDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoilDefinitions_ScaleDefinitions_ScaleId",
                        column: x => x.ScaleId,
                        principalTable: "ScaleDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterCoils",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    CoilDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StorytellerNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterCoils", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterCoils_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterCoils_CoilDefinitions_CoilDefinitionId",
                        column: x => x.CoilDefinitionId,
                        principalTable: "CoilDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_ChosenMysteryScaleId",
                table: "Characters",
                column: "ChosenMysteryScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_PendingChosenMysteryScaleId",
                table: "Characters",
                column: "PendingChosenMysteryScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterCoils_CharacterId",
                table: "CharacterCoils",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterCoils_CoilDefinitionId",
                table: "CharacterCoils",
                column: "CoilDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CoilDefinitions_PrerequisiteCoilId",
                table: "CoilDefinitions",
                column: "PrerequisiteCoilId");

            migrationBuilder.CreateIndex(
                name: "IX_CoilDefinitions_ScaleId",
                table: "CoilDefinitions",
                column: "ScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_ScaleDefinitions_Name",
                table: "ScaleDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_ScaleDefinitions_ChosenMysteryScaleId",
                table: "Characters",
                column: "ChosenMysteryScaleId",
                principalTable: "ScaleDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_ScaleDefinitions_PendingChosenMysteryScaleId",
                table: "Characters",
                column: "PendingChosenMysteryScaleId",
                principalTable: "ScaleDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_ScaleDefinitions_ChosenMysteryScaleId",
                table: "Characters");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_ScaleDefinitions_PendingChosenMysteryScaleId",
                table: "Characters");

            migrationBuilder.DropTable(
                name: "CharacterCoils");

            migrationBuilder.DropTable(
                name: "CoilDefinitions");

            migrationBuilder.DropTable(
                name: "ScaleDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_Characters_ChosenMysteryScaleId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_PendingChosenMysteryScaleId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ChosenMysteryScaleId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "HasCrucibleRitualAccess",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "PendingChosenMysteryScaleId",
                table: "Characters");
        }
    }
}
