using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase19DisciplineAcquisitionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "XpLedger",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BloodlineId",
                table: "Disciplines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanLearnIndependently",
                table: "Disciplines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CovenantId",
                table: "Disciplines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBloodlineDiscipline",
                table: "Disciplines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCovenantDiscipline",
                table: "Disciplines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNecromancy",
                table: "Disciplines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresMentorBloodToLearn",
                table: "Disciplines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PoolDefinitionJson",
                table: "DisciplinePowers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Disciplines_BloodlineId",
                table: "Disciplines",
                column: "BloodlineId");

            migrationBuilder.CreateIndex(
                name: "IX_Disciplines_CovenantId",
                table: "Disciplines",
                column: "CovenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Disciplines_BloodlineDefinitions_BloodlineId",
                table: "Disciplines",
                column: "BloodlineId",
                principalTable: "BloodlineDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Disciplines_CovenantDefinitions_CovenantId",
                table: "Disciplines",
                column: "CovenantId",
                principalTable: "CovenantDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Disciplines_BloodlineDefinitions_BloodlineId",
                table: "Disciplines");

            migrationBuilder.DropForeignKey(
                name: "FK_Disciplines_CovenantDefinitions_CovenantId",
                table: "Disciplines");

            migrationBuilder.DropIndex(
                name: "IX_Disciplines_BloodlineId",
                table: "Disciplines");

            migrationBuilder.DropIndex(
                name: "IX_Disciplines_CovenantId",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "XpLedger");

            migrationBuilder.DropColumn(
                name: "BloodlineId",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "CanLearnIndependently",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "CovenantId",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "IsBloodlineDiscipline",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "IsCovenantDiscipline",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "IsNecromancy",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "RequiresMentorBloodToLearn",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "PoolDefinitionJson",
                table: "DisciplinePowers");
        }
    }
}
