using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class SorceryRiteRequiresElderAndRemoveOrdo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DELETE FROM ""CharacterRites"" WHERE ""SorceryRiteDefinitionId"" IN (SELECT ""Id"" FROM ""SorceryRiteDefinitions"" WHERE ""SorceryType"" = 3);");

            migrationBuilder.Sql(
                @"DELETE FROM ""SorceryRiteDefinitions"" WHERE ""SorceryType"" = 3;");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresElder",
                table: "SorceryRiteDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresElder",
                table: "SorceryRiteDefinitions");
        }
    }
}
