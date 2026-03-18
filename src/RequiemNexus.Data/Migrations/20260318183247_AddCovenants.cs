using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCovenants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CovenantId",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CovenantJoinStatus",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CovenantLeaveRequestedAt",
                table: "Characters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CovenantDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsPlayable = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CovenantDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CovenantDefinitionMerits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CovenantDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    MeritId = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CovenantDefinitionMerits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CovenantDefinitionMerits_CovenantDefinitions_CovenantDefini~",
                        column: x => x.CovenantDefinitionId,
                        principalTable: "CovenantDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CovenantDefinitionMerits_Merits_MeritId",
                        column: x => x.MeritId,
                        principalTable: "Merits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_CovenantId",
                table: "Characters",
                column: "CovenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CovenantDefinitionMerits_CovenantDefinitionId",
                table: "CovenantDefinitionMerits",
                column: "CovenantDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CovenantDefinitionMerits_MeritId",
                table: "CovenantDefinitionMerits",
                column: "MeritId");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_CovenantDefinitions_CovenantId",
                table: "Characters",
                column: "CovenantId",
                principalTable: "CovenantDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_CovenantDefinitions_CovenantId",
                table: "Characters");

            migrationBuilder.DropTable(
                name: "CovenantDefinitionMerits");

            migrationBuilder.DropTable(
                name: "CovenantDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_Characters_CovenantId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CovenantId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CovenantJoinStatus",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CovenantLeaveRequestedAt",
                table: "Characters");
        }
    }
}
