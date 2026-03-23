using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase11ArmoryRefinements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "WeaponSlotPoints",
                table: "WeaponAssets",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "ConcealmentRating",
                table: "WeaponAssets",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DamageType",
                table: "WeaponAssets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustom",
                table: "CharacterAssets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastProcurementDate",
                table: "CharacterAssets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WasAcquiredViaReach",
                table: "CharacterAssets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ItemSize",
                table: "ArmorAssets",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssetModifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Availability = table.Column<int>(type: "integer", nullable: false),
                    ModifierEffectJson = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetModifiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterAssetModifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterAssetId = table.Column<int>(type: "integer", nullable: false),
                    AssetModifierId = table.Column<int>(type: "integer", nullable: false),
                    CustomName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterAssetModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterAssetModifiers_AssetModifiers_AssetModifierId",
                        column: x => x.AssetModifierId,
                        principalTable: "AssetModifiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharacterAssetModifiers_CharacterAssets_CharacterAssetId",
                        column: x => x.CharacterAssetId,
                        principalTable: "CharacterAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_CharacterAsset_EquippedOrBackpack",
                table: "CharacterAssets",
                sql: "(NOT \"IsEquipped\" OR \"BackpackSlotIndex\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_AssetModifiers_Slug",
                table: "AssetModifiers",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAssetModifiers_AssetModifierId",
                table: "CharacterAssetModifiers",
                column: "AssetModifierId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAssetModifiers_CharacterAssetId_AssetModifierId",
                table: "CharacterAssetModifiers",
                columns: new[] { "CharacterAssetId", "AssetModifierId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterAssetModifiers");

            migrationBuilder.DropTable(
                name: "AssetModifiers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CharacterAsset_EquippedOrBackpack",
                table: "CharacterAssets");

            migrationBuilder.DropColumn(
                name: "ConcealmentRating",
                table: "WeaponAssets");

            migrationBuilder.DropColumn(
                name: "DamageType",
                table: "WeaponAssets");

            migrationBuilder.DropColumn(
                name: "IsCustom",
                table: "CharacterAssets");

            migrationBuilder.DropColumn(
                name: "LastProcurementDate",
                table: "CharacterAssets");

            migrationBuilder.DropColumn(
                name: "WasAcquiredViaReach",
                table: "CharacterAssets");

            migrationBuilder.DropColumn(
                name: "ItemSize",
                table: "ArmorAssets");

            migrationBuilder.AlterColumn<int>(
                name: "WeaponSlotPoints",
                table: "WeaponAssets",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);
        }
    }
}
