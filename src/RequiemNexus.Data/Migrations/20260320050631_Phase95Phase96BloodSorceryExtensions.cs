using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase95Phase96BloodSorceryExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RequiredCovenantId",
                table: "SorceryRiteDefinitions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "RequiredClanId",
                table: "SorceryRiteDefinitions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequirementsJson",
                table: "SorceryRiteDefinitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsOrdoRituals",
                table: "CovenantDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HumanityStains",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SorceryRiteDefinitions_RequiredClanId",
                table: "SorceryRiteDefinitions",
                column: "RequiredClanId");

            migrationBuilder.AddForeignKey(
                name: "FK_SorceryRiteDefinitions_Clans_RequiredClanId",
                table: "SorceryRiteDefinitions",
                column: "RequiredClanId",
                principalTable: "Clans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SorceryRiteDefinitions_Clans_RequiredClanId",
                table: "SorceryRiteDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_SorceryRiteDefinitions_RequiredClanId",
                table: "SorceryRiteDefinitions");

            migrationBuilder.DropColumn(
                name: "RequiredClanId",
                table: "SorceryRiteDefinitions");

            migrationBuilder.DropColumn(
                name: "RequirementsJson",
                table: "SorceryRiteDefinitions");

            migrationBuilder.DropColumn(
                name: "SupportsOrdoRituals",
                table: "CovenantDefinitions");

            migrationBuilder.DropColumn(
                name: "HumanityStains",
                table: "Characters");

            migrationBuilder.AlterColumn<int>(
                name: "RequiredCovenantId",
                table: "SorceryRiteDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
