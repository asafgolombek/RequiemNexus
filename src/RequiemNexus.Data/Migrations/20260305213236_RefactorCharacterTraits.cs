using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCharacterTraits : Migration
    {
        private const string IntegerType = "INTEGER";
        private const string CharactersTable = "Characters";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)

        {
            migrationBuilder.CreateTable(
                name: "CharacterAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: IntegerType, nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: IntegerType, nullable: false),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    Category = table.Column<int>(type: IntegerType, nullable: false),
                    Rating = table.Column<int>(type: IntegerType, nullable: false)

                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterAttributes_Characters_CharacterId", // Constraint name is fine as literal
                        column: x => x.CharacterId,
                        principalTable: CharactersTable,
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);

                });

            migrationBuilder.CreateTable(
                name: "CharacterSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: IntegerType, nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: IntegerType, nullable: false),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    Category = table.Column<int>(type: IntegerType, nullable: false),
                    Rating = table.Column<int>(type: IntegerType, nullable: false)
,
                    Specialty = table.Column<string>(maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterSkills_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: CharactersTable,
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // DATA MIGRATION: ATTRIBUTES
            // All identifiers are double-quoted — PostgreSQL folds unquoted names to lowercase,
            // but EF Core creates tables/columns with quoted (case-sensitive) identifiers.
            migrationBuilder.Sql(@"INSERT INTO ""CharacterAttributes"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Intelligence', 0, ""Intelligence"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterAttributes"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Wits', 0, ""Wits"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterAttributes"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Resolve', 0, ""Resolve"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterAttributes"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Strength', 1, ""Strength"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterAttributes"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Dexterity', 1, ""Dexterity"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterAttributes"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Stamina', 1, ""Stamina"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterAttributes"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Presence', 2, ""Presence"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterAttributes"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Manipulation', 2, ""Manipulation"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterAttributes"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Composure', 2, ""Composure"" FROM ""Characters"";");

            // DATA MIGRATION: SKILLS
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Academics', 0, ""Academics"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Computer', 0, ""Computer"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Crafts', 0, ""Crafts"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Investigation', 0, ""Investigation"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Medicine', 0, ""Medicine"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Occult', 0, ""Occult"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Politics', 0, ""Politics"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Science', 0, ""Science"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Athletics', 1, ""Athletics"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Brawl', 1, ""Brawl"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Drive', 1, ""Drive"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Firearms', 1, ""Firearms"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Larceny', 1, ""Larceny"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Stealth', 1, ""Stealth"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Survival', 1, ""Survival"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Weaponry', 1, ""Weaponry"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'AnimalKen', 2, ""AnimalKen"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Empathy', 2, ""Empathy"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Expression', 2, ""Expression"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Intimidation', 2, ""Intimidation"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Persuasion', 2, ""Persuasion"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Socialize', 2, ""Socialize"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Streetwise', 2, ""Streetwise"" FROM ""Characters"";");
            migrationBuilder.Sql(@"INSERT INTO ""CharacterSkills"" (""CharacterId"", ""Name"", ""Category"", ""Rating"") SELECT ""Id"", 'Subterfuge', 2, ""Subterfuge"" FROM ""Characters"";");

            migrationBuilder.DropColumn(
                name: "Academics",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "AnimalKen",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Athletics",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Brawl",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Composure",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Computer",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Crafts",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Dexterity",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Drive",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Empathy",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Expression",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Firearms",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Intelligence",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Intimidation",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Investigation",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Larceny",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Manipulation",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Medicine",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Occult",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Persuasion",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Politics",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Presence",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Resolve",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Science",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Socialize",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Stamina",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Stealth",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Streetwise",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Strength",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Subterfuge",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Survival",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Weaponry",
                table: CharactersTable);

            migrationBuilder.DropColumn(
                name: "Wits",
                table: CharactersTable);


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
                table: "Characters", // Wait, I should use the constant here too? Yes.
                type: IntegerType,

                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AnimalKen",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Athletics",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Brawl",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Composure",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Computer",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Crafts",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Dexterity",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Drive",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Empathy",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Expression",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Firearms",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Intelligence",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Intimidation",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Investigation",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Larceny",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Manipulation",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Medicine",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Occult",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Persuasion",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Politics",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Presence",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Resolve",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Science",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Socialize",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Stamina",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Stealth",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Streetwise",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Strength",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Subterfuge",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Survival",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Weaponry",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Wits",
                table: CharactersTable,
                type: IntegerType,
                nullable: false,
                defaultValue: 0);
        }
    }
}

