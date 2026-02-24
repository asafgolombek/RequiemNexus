using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDisciplineAndMeritModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "CharacterMerits");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "CharacterDisciplines");

            migrationBuilder.AddColumn<int>(
                name: "MeritId",
                table: "CharacterMerits",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Specification",
                table: "CharacterMerits",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisciplineId",
                table: "CharacterDisciplines",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Disciplines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disciplines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Merits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ValidRatings = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RequiresSpecification = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanBePurchasedMultipleTimes = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Merits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisciplinePowers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisciplineId = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    DicePool = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Cost = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisciplinePowers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisciplinePowers_Disciplines_DisciplineId",
                        column: x => x.DisciplineId,
                        principalTable: "Disciplines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterMerits_MeritId",
                table: "CharacterMerits",
                column: "MeritId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterDisciplines_DisciplineId",
                table: "CharacterDisciplines",
                column: "DisciplineId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinePowers_DisciplineId",
                table: "DisciplinePowers",
                column: "DisciplineId");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterDisciplines_Disciplines_DisciplineId",
                table: "CharacterDisciplines",
                column: "DisciplineId",
                principalTable: "Disciplines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterMerits_Merits_MeritId",
                table: "CharacterMerits",
                column: "MeritId",
                principalTable: "Merits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterDisciplines_Disciplines_DisciplineId",
                table: "CharacterDisciplines");

            migrationBuilder.DropForeignKey(
                name: "FK_CharacterMerits_Merits_MeritId",
                table: "CharacterMerits");

            migrationBuilder.DropTable(
                name: "DisciplinePowers");

            migrationBuilder.DropTable(
                name: "Merits");

            migrationBuilder.DropTable(
                name: "Disciplines");

            migrationBuilder.DropIndex(
                name: "IX_CharacterMerits_MeritId",
                table: "CharacterMerits");

            migrationBuilder.DropIndex(
                name: "IX_CharacterDisciplines_DisciplineId",
                table: "CharacterDisciplines");

            migrationBuilder.DropColumn(
                name: "MeritId",
                table: "CharacterMerits");

            migrationBuilder.DropColumn(
                name: "Specification",
                table: "CharacterMerits");

            migrationBuilder.DropColumn(
                name: "DisciplineId",
                table: "CharacterDisciplines");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CharacterMerits",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CharacterDisciplines",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
