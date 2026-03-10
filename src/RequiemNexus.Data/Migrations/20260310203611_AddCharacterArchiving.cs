using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Characters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRetired",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetiredAt",
                table: "Characters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CharacterNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    CampaignId = table.Column<int>(type: "INTEGER", nullable: true),
                    AuthorUserId = table.Column<string>(type: "TEXT", nullable: false),
                    IsStorytellerPrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterNotes_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiceMacros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DicePool = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiceMacros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiceMacros_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterNotes_CampaignId",
                table: "CharacterNotes",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterNotes_CharacterId",
                table: "CharacterNotes",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_DiceMacros_CharacterId",
                table: "DiceMacros",
                column: "CharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterNotes");

            migrationBuilder.DropTable(
                name: "DiceMacros");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "IsRetired",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "RetiredAt",
                table: "Characters");
        }
    }
}
