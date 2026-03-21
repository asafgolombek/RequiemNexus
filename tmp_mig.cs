using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    public partial class EncounterToolFull : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "IsHeld", table: "InitiativeEntries", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "IsRevealed", table: "InitiativeEntries", type: "boolean", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<string>(name: "MaskedDisplayName", table: "InitiativeEntries", type: "character varying(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<int>(name: "NpcHealthBoxes", table: "InitiativeEntries", type: "integer", nullable: false, defaultValue: 7);
            migrationBuilder.AddColumn<string>(name: "NpcHealthDamage", table: "InitiativeEntries", type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<int>(name: "CurrentRound", table: "CombatEncounters", type: "integer", nullable: false, defaultValue: 1);
            migrationBuilder.AddColumn<bool>(name: "IsDraft", table: "CombatEncounters", type: "boolean", nullable: false, defaultValue: false);
        }
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
