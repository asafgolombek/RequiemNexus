using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase12WebOfNight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CharacterConditions_CharacterId",
                table: "CharacterConditions");

            migrationBuilder.AddColumn<int>(
                name: "SireCharacterId",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SireDisplayName",
                table: "Characters",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SireNpcId",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceTag",
                table: "CharacterConditions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BloodBonds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChronicleId = table.Column<int>(type: "integer", nullable: false),
                    ThrallCharacterId = table.Column<int>(type: "integer", nullable: false),
                    RegnantCharacterId = table.Column<int>(type: "integer", nullable: true),
                    RegnantNpcId = table.Column<int>(type: "integer", nullable: true),
                    RegnantDisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    RegnantKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    LastFedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloodBonds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloodBonds_Campaigns_ChronicleId",
                        column: x => x.ChronicleId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BloodBonds_Characters_RegnantCharacterId",
                        column: x => x.RegnantCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BloodBonds_Characters_ThrallCharacterId",
                        column: x => x.ThrallCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BloodBonds_ChronicleNpcs_RegnantNpcId",
                        column: x => x.RegnantNpcId,
                        principalTable: "ChronicleNpcs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ghouls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChronicleId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RegnantCharacterId = table.Column<int>(type: "integer", nullable: true),
                    RegnantNpcId = table.Column<int>(type: "integer", nullable: true),
                    RegnantDisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    LastFedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VitaeInSystem = table.Column<int>(type: "integer", nullable: false),
                    ApparentAge = table.Column<int>(type: "integer", nullable: true),
                    ActualAge = table.Column<int>(type: "integer", nullable: true),
                    AccessibleDisciplinesJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsReleased = table.Column<bool>(type: "boolean", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ghouls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ghouls_Campaigns_ChronicleId",
                        column: x => x.ChronicleId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ghouls_Characters_RegnantCharacterId",
                        column: x => x.RegnantCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ghouls_ChronicleNpcs_RegnantNpcId",
                        column: x => x.RegnantNpcId,
                        principalTable: "ChronicleNpcs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PredatoryAuraContests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChronicleId = table.Column<int>(type: "integer", nullable: false),
                    AttackerCharacterId = table.Column<int>(type: "integer", nullable: false),
                    DefenderCharacterId = table.Column<int>(type: "integer", nullable: false),
                    AttackerBloodPotency = table.Column<int>(type: "integer", nullable: false),
                    DefenderBloodPotency = table.Column<int>(type: "integer", nullable: false),
                    AttackerSuccesses = table.Column<int>(type: "integer", nullable: false),
                    DefenderSuccesses = table.Column<int>(type: "integer", nullable: false),
                    WinnerId = table.Column<int>(type: "integer", nullable: true),
                    OutcomeApplied = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsLashOut = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredatoryAuraContests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PredatoryAuraContests_Campaigns_ChronicleId",
                        column: x => x.ChronicleId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PredatoryAuraContests_Characters_AttackerCharacterId",
                        column: x => x.AttackerCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PredatoryAuraContests_Characters_DefenderCharacterId",
                        column: x => x.DefenderCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PredatoryAuraContests_Characters_WinnerId",
                        column: x => x.WinnerId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_SireCharacterId",
                table: "Characters",
                column: "SireCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_SireNpcId",
                table: "Characters",
                column: "SireNpcId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterConditions_CharacterId_ConditionType_IsResolved_So~",
                table: "CharacterConditions",
                columns: new[] { "CharacterId", "ConditionType", "IsResolved", "SourceTag" });

            migrationBuilder.CreateIndex(
                name: "IX_BloodBonds_ChronicleId",
                table: "BloodBonds",
                column: "ChronicleId");

            migrationBuilder.CreateIndex(
                name: "IX_BloodBonds_ChronicleId_ThrallCharacterId_RegnantKey",
                table: "BloodBonds",
                columns: new[] { "ChronicleId", "ThrallCharacterId", "RegnantKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BloodBonds_RegnantCharacterId",
                table: "BloodBonds",
                column: "RegnantCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_BloodBonds_RegnantNpcId",
                table: "BloodBonds",
                column: "RegnantNpcId");

            migrationBuilder.CreateIndex(
                name: "IX_BloodBonds_ThrallCharacterId",
                table: "BloodBonds",
                column: "ThrallCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Ghouls_ChronicleId",
                table: "Ghouls",
                column: "ChronicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Ghouls_RegnantCharacterId",
                table: "Ghouls",
                column: "RegnantCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Ghouls_RegnantNpcId",
                table: "Ghouls",
                column: "RegnantNpcId");

            migrationBuilder.CreateIndex(
                name: "IX_PredatoryAuraContests_AttackerCharacterId",
                table: "PredatoryAuraContests",
                column: "AttackerCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_PredatoryAuraContests_ChronicleId",
                table: "PredatoryAuraContests",
                column: "ChronicleId");

            migrationBuilder.CreateIndex(
                name: "IX_PredatoryAuraContests_DefenderCharacterId",
                table: "PredatoryAuraContests",
                column: "DefenderCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_PredatoryAuraContests_WinnerId",
                table: "PredatoryAuraContests",
                column: "WinnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Characters_SireCharacterId",
                table: "Characters",
                column: "SireCharacterId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_ChronicleNpcs_SireNpcId",
                table: "Characters",
                column: "SireNpcId",
                principalTable: "ChronicleNpcs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Characters_SireCharacterId",
                table: "Characters");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_ChronicleNpcs_SireNpcId",
                table: "Characters");

            migrationBuilder.DropTable(
                name: "BloodBonds");

            migrationBuilder.DropTable(
                name: "Ghouls");

            migrationBuilder.DropTable(
                name: "PredatoryAuraContests");

            migrationBuilder.DropIndex(
                name: "IX_Characters_SireCharacterId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_SireNpcId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_CharacterConditions_CharacterId_ConditionType_IsResolved_So~",
                table: "CharacterConditions");

            migrationBuilder.DropColumn(
                name: "SireCharacterId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "SireDisplayName",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "SireNpcId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "SourceTag",
                table: "CharacterConditions");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterConditions_CharacterId",
                table: "CharacterConditions",
                column: "CharacterId");
        }
    }
}
