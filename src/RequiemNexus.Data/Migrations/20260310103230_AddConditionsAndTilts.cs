using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConditionsAndTilts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterConditions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(nullable: false),
                    ConditionType = table.Column<int>(nullable: false),
                    CustomName = table.Column<string>(maxLength: 100, nullable: true),
                    Description = table.Column<string>(maxLength: 1000, nullable: true),
                    AppliedAt = table.Column<DateTime>(nullable: false),
                    ResolvedAt = table.Column<DateTime>(nullable: true),
                    IsResolved = table.Column<bool>(nullable: false),
                    AwardsBeat = table.Column<bool>(nullable: false),
                    AppliedByUserId = table.Column<string>(maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterConditions_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterTilts",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(nullable: false),
                    EncounterId = table.Column<int>(nullable: true),
                    TiltType = table.Column<int>(nullable: false),
                    CustomName = table.Column<string>(maxLength: 100, nullable: true),
                    AppliedAt = table.Column<DateTime>(nullable: false),
                    RemovedAt = table.Column<DateTime>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false),
                    AppliedByUserId = table.Column<string>(maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterTilts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterTilts_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterConditions_CharacterId",
                table: "CharacterConditions",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTilts_CharacterId",
                table: "CharacterTilts",
                column: "CharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterConditions");

            migrationBuilder.DropTable(
                name: "CharacterTilts");
        }
    }
}
