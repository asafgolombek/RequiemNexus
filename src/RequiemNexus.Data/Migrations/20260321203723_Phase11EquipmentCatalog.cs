using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase11EquipmentCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Equipment",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Equipment",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArmorBallisticRating",
                table: "Equipment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ArmorCoverage",
                table: "Equipment",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArmorDefenseModifier",
                table: "Equipment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ArmorEra",
                table: "Equipment",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ArmorIsConcealable",
                table: "Equipment",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ArmorSpeedModifier",
                table: "Equipment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AssistsSkillName",
                table: "Equipment",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Availability",
                table: "Equipment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ClipInfo",
                table: "Equipment",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiceBonusMax",
                table: "Equipment",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiceBonusMin",
                table: "Equipment",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InitiativeModifier",
                table: "Equipment",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsIllicit",
                table: "Equipment",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRangedWeapon",
                table: "Equipment",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ItemCategory",
                table: "Equipment",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemDurability",
                table: "Equipment",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemSize",
                table: "Equipment",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ranges",
                table: "Equipment",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecurringResourcesCost",
                table: "Equipment",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Equipment",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StrengthRequirement",
                table: "Equipment",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UsesBrawlForAttacks",
                table: "Equipment",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WeaponSpecialNotes",
                table: "Equipment",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStructure",
                table: "CharacterEquipments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEquipped",
                table: "CharacterEquipments",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ReadySlotIndex",
                table: "CharacterEquipments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Slug",
                table: "Equipment",
                column: "Slug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Equipment_Slug",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "ArmorBallisticRating",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "ArmorCoverage",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "ArmorDefenseModifier",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "ArmorEra",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "ArmorIsConcealable",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "ArmorSpeedModifier",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "AssistsSkillName",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Availability",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "ClipInfo",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "DiceBonusMax",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "DiceBonusMin",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "InitiativeModifier",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "IsIllicit",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "IsRangedWeapon",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "ItemCategory",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "ItemDurability",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "ItemSize",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Ranges",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "RecurringResourcesCost",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "StrengthRequirement",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "UsesBrawlForAttacks",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "WeaponSpecialNotes",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "CurrentStructure",
                table: "CharacterEquipments");

            migrationBuilder.DropColumn(
                name: "IsEquipped",
                table: "CharacterEquipments");

            migrationBuilder.DropColumn(
                name: "ReadySlotIndex",
                table: "CharacterEquipments");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Equipment",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Equipment",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);
        }
    }
}
