using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase1Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Dirge",
                table: "Characters",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Mask",
                table: "Characters",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CharacterAspirations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    IsLongTerm = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterAspirations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterAspirations_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterBanes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterBanes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterBanes_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAspirations_CharacterId",
                table: "CharacterAspirations",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterBanes_CharacterId",
                table: "CharacterBanes",
                column: "CharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterAspirations");

            migrationBuilder.DropTable(
                name: "CharacterBanes");

            migrationBuilder.DropColumn(
                name: "Dirge",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Mask",
                table: "Characters");
        }
    }
}
