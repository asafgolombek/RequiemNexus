using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
#pragma warning disable S1192 // String literals should not be duplicated
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
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Specification",
                table: "CharacterMerits",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisciplineId",
                table: "CharacterDisciplines",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Disciplines",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Description = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disciplines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Merits",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Description = table.Column<string>(nullable: false),
                    ValidRatings = table.Column<string>(maxLength: 50, nullable: false),
                    RequiresSpecification = table.Column<bool>(nullable: false),
                    CanBePurchasedMultipleTimes = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Merits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisciplinePowers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisciplineId = table.Column<int>(nullable: false),
                    Level = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Description = table.Column<string>(nullable: false),
                    DicePool = table.Column<string>(maxLength: 100, nullable: false),
                    Cost = table.Column<string>(maxLength: 100, nullable: false)
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
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CharacterDisciplines",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
