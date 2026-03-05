using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCharacterTraits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterAttributes_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Specialty = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterSkills_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // DATA MIGRATION: ATTRIBUTES
            migrationBuilder.Sql("INSERT INTO CharacterAttributes (CharacterId, Name, Category, Rating) SELECT Id, 'Intelligence', 0, Intelligence FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterAttributes (CharacterId, Name, Category, Rating) SELECT Id, 'Wits', 0, Wits FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterAttributes (CharacterId, Name, Category, Rating) SELECT Id, 'Resolve', 0, Resolve FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterAttributes (CharacterId, Name, Category, Rating) SELECT Id, 'Strength', 1, Strength FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterAttributes (CharacterId, Name, Category, Rating) SELECT Id, 'Dexterity', 1, Dexterity FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterAttributes (CharacterId, Name, Category, Rating) SELECT Id, 'Stamina', 1, Stamina FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterAttributes (CharacterId, Name, Category, Rating) SELECT Id, 'Presence', 2, Presence FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterAttributes (CharacterId, Name, Category, Rating) SELECT Id, 'Manipulation', 2, Manipulation FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterAttributes (CharacterId, Name, Category, Rating) SELECT Id, 'Composure', 2, Composure FROM Characters;");

            // DATA MIGRATION: SKILLS
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Academics', 0, Academics FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Computer', 0, Computer FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Crafts', 0, Crafts FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Investigation', 0, Investigation FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Medicine', 0, Medicine FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Occult', 0, Occult FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Politics', 0, Politics FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Science', 0, Science FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Athletics', 1, Athletics FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Brawl', 1, Brawl FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Drive', 1, Drive FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Firearms', 1, Firearms FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Larceny', 1, Larceny FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Stealth', 1, Stealth FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Survival', 1, Survival FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Weaponry', 1, Weaponry FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'AnimalKen', 2, AnimalKen FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Empathy', 2, Empathy FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Expression', 2, Expression FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Intimidation', 2, Intimidation FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Persuasion', 2, Persuasion FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Socialize', 2, Socialize FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Streetwise', 2, Streetwise FROM Characters;");
            migrationBuilder.Sql("INSERT INTO CharacterSkills (CharacterId, Name, Category, Rating) SELECT Id, 'Subterfuge', 2, Subterfuge FROM Characters;");

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

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAttributes_CharacterId_Name",
                table: "CharacterAttributes",
                columns: new[] { "CharacterId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_CharacterId_Name",
                table: "CharacterSkills",
                columns: new[] { "CharacterId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterAttributes");

            migrationBuilder.DropTable(
                name: "CharacterSkills");

            migrationBuilder.AddColumn<int>(
                name: "Academics",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AnimalKen",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Athletics",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Brawl",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Composure",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Computer",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Crafts",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Dexterity",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Drive",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Empathy",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Expression",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Firearms",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Intelligence",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Intimidation",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Investigation",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Larceny",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Manipulation",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Medicine",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Occult",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Persuasion",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Politics",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Presence",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Resolve",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Science",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Socialize",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Stamina",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Stealth",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Streetwise",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Strength",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Subterfuge",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Survival",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Weaponry",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Wits",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
