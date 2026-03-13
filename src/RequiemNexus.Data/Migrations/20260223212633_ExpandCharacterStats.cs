using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
#pragma warning disable S1192 // String literals should not be duplicated
    /// <inheritdoc />
    public partial class ExpandCharacterStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Academics",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AnimalKen",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Athletics",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Brawl",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Composure",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Computer",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Crafts",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Dexterity",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Drive",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Empathy",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Expression",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Firearms",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Intelligence",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Intimidation",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Investigation",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Larceny",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Manipulation",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Medicine",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Occult",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Persuasion",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Politics",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Presence",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Resolve",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Science",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Socialize",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Stamina",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Stealth",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Streetwise",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Strength",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Subterfuge",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Survival",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Weaponry",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Wits",
                table: "Characters",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CharacterDisciplines",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Rating = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterDisciplines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterDisciplines_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterMerits",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Rating = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterMerits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterMerits_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterDisciplines_CharacterId",
                table: "CharacterDisciplines",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterMerits_CharacterId",
                table: "CharacterMerits",
                column: "CharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterDisciplines");

            migrationBuilder.DropTable(
                name: "CharacterMerits");

            migrationBuilder.DropColumn(
                name: "Academics",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "AnimalKen",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Athletics",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Brawl",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Composure",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Computer",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Crafts",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Dexterity",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Drive",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Empathy",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Expression",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Firearms",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Intelligence",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Intimidation",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Investigation",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Larceny",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Manipulation",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Medicine",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Occult",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Persuasion",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Politics",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Presence",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Resolve",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Science",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Socialize",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Stamina",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Stealth",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Streetwise",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Strength",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Subterfuge",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Survival",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Weaponry",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Wits",
                table: "Characters");
        }
    }
}
