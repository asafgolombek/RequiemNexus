using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase15TorporState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastStarvationNotifiedAt",
                table: "Characters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TorporSince",
                table: "Characters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTilts_CharacterId_TiltType",
                table: "CharacterTilts",
                columns: new[] { "CharacterId", "TiltType" },
                unique: true,
                filter: "\"IsActive\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CharacterTilts_CharacterId_TiltType",
                table: "CharacterTilts");

            migrationBuilder.DropColumn(
                name: "LastStarvationNotifiedAt",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "TorporSince",
                table: "Characters");
        }
    }
}
