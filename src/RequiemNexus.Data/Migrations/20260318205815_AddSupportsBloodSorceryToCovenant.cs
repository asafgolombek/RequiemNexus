using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportsBloodSorceryToCovenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SupportsBloodSorcery",
                table: "CovenantDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"UPDATE ""CovenantDefinitions"" SET ""SupportsBloodSorcery"" = true WHERE ""Name"" IN ('The Circle of the Crone', 'The Lancea et Sanctum');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupportsBloodSorcery",
                table: "CovenantDefinitions");
        }
    }
}
