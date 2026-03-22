using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase11AssetTpt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Weight = table.Column<float>(type: "real", nullable: false),
                    Cost = table.Column<int>(type: "integer", nullable: false),
                    Availability = table.Column<int>(type: "integer", nullable: false),
                    IsIllicit = table.Column<bool>(type: "boolean", nullable: false),
                    IsListedInCatalog = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArmorAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ArmorRating = table.Column<int>(type: "integer", nullable: false),
                    ArmorBallisticRating = table.Column<int>(type: "integer", nullable: false),
                    ArmorDefenseModifier = table.Column<int>(type: "integer", nullable: false),
                    ArmorSpeedModifier = table.Column<int>(type: "integer", nullable: false),
                    Penalty = table.Column<int>(type: "integer", nullable: false),
                    StrengthRequirement = table.Column<int>(type: "integer", nullable: true),
                    ArmorEra = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ArmorCoverage = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ArmorIsConcealable = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArmorAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArmorAssets_Assets_Id",
                        column: x => x.Id,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetCapabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssetId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    AssistsSkillName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DiceBonusMin = table.Column<int>(type: "integer", nullable: true),
                    DiceBonusMax = table.Column<int>(type: "integer", nullable: true),
                    WeaponProfileAssetId = table.Column<int>(type: "integer", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCapabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetCapabilities_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetCapabilities_Assets_WeaponProfileAssetId",
                        column: x => x.WeaponProfileAssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    AssetId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    IsEquipped = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ReadySlotIndex = table.Column<int>(type: "integer", nullable: true),
                    CurrentStructure = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterAssets_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterAssets_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ItemCategory = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AssistsSkillName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DiceBonusMin = table.Column<int>(type: "integer", nullable: true),
                    DiceBonusMax = table.Column<int>(type: "integer", nullable: true),
                    ItemSize = table.Column<int>(type: "integer", nullable: true),
                    ItemDurability = table.Column<int>(type: "integer", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentAssets_Assets_Id",
                        column: x => x.Id,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingAssetProcurements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    AssetId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PlayerNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StorytellerNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingAssetProcurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingAssetProcurements_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PendingAssetProcurements_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    AssistsSkillName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DiceBonusMin = table.Column<int>(type: "integer", nullable: true),
                    DiceBonusMax = table.Column<int>(type: "integer", nullable: true),
                    RecurringResourcesCost = table.Column<int>(type: "integer", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceAssets_Assets_Id",
                        column: x => x.Id,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeaponAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Damage = table.Column<int>(type: "integer", nullable: false),
                    InitiativeModifier = table.Column<int>(type: "integer", nullable: true),
                    StrengthRequirement = table.Column<int>(type: "integer", nullable: true),
                    Ranges = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ClipInfo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IsRangedWeapon = table.Column<bool>(type: "boolean", nullable: false),
                    UsesBrawlForAttacks = table.Column<bool>(type: "boolean", nullable: false),
                    WeaponSpecialNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HasAutofire = table.Column<bool>(type: "boolean", nullable: false),
                    HasNineAgain = table.Column<bool>(type: "boolean", nullable: false),
                    ArmorPiercingRating = table.Column<int>(type: "integer", nullable: false),
                    HasStun = table.Column<bool>(type: "boolean", nullable: false),
                    ItemSize = table.Column<int>(type: "integer", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeaponAssets_Assets_Id",
                        column: x => x.Id,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetCapabilities_AssetId",
                table: "AssetCapabilities",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetCapabilities_WeaponProfileAssetId",
                table: "AssetCapabilities",
                column: "WeaponProfileAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Slug",
                table: "Assets",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAssets_AssetId",
                table: "CharacterAssets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAssets_CharacterId",
                table: "CharacterAssets",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingAssetProcurements_AssetId",
                table: "PendingAssetProcurements",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingAssetProcurements_CharacterId_Status",
                table: "PendingAssetProcurements",
                columns: new[] { "CharacterId", "Status" });

            migrationBuilder.Sql(@"
INSERT INTO ""Assets"" (""Id"", ""Slug"", ""Name"", ""Kind"", ""Description"", ""Weight"", ""Cost"", ""Availability"", ""IsIllicit"", ""IsListedInCatalog"")
OVERRIDING SYSTEM VALUE
SELECT e.""Id"", e.""Slug"", e.""Name"", e.""Type"", e.""Description"", e.""Weight"", e.""Cost"", e.""Availability"", e.""IsIllicit"",
  CASE WHEN e.""Slug"" = 'vtm2e:wp:crowbar-profile' THEN FALSE ELSE TRUE END
FROM ""Equipment"" e;

INSERT INTO ""WeaponAssets"" (""Id"", ""Damage"", ""InitiativeModifier"", ""StrengthRequirement"", ""Ranges"", ""ClipInfo"", ""IsRangedWeapon"", ""UsesBrawlForAttacks"", ""WeaponSpecialNotes"", ""HasAutofire"", ""HasNineAgain"", ""ArmorPiercingRating"", ""HasStun"", ""ItemSize"")
SELECT ""Id"", ""Damage"", ""InitiativeModifier"", ""StrengthRequirement"", ""Ranges"", ""ClipInfo"", ""IsRangedWeapon"", ""UsesBrawlForAttacks"", ""WeaponSpecialNotes"", FALSE, FALSE, 0, FALSE, ""ItemSize""
FROM ""Equipment"" WHERE ""Type"" = 0;

INSERT INTO ""ArmorAssets"" (""Id"", ""ArmorRating"", ""ArmorBallisticRating"", ""ArmorDefenseModifier"", ""ArmorSpeedModifier"", ""Penalty"", ""StrengthRequirement"", ""ArmorEra"", ""ArmorCoverage"", ""ArmorIsConcealable"")
SELECT ""Id"", ""ArmorRating"", ""ArmorBallisticRating"", ""ArmorDefenseModifier"", ""ArmorSpeedModifier"", ""Penalty"", ""StrengthRequirement"", ""ArmorEra"", ""ArmorCoverage"", ""ArmorIsConcealable""
FROM ""Equipment"" WHERE ""Type"" = 1;

INSERT INTO ""EquipmentAssets"" (""Id"", ""ItemCategory"", ""AssistsSkillName"", ""DiceBonusMin"", ""DiceBonusMax"", ""ItemSize"", ""ItemDurability"")
SELECT ""Id"", ""ItemCategory"", ""AssistsSkillName"", ""DiceBonusMin"", ""DiceBonusMax"", ""ItemSize"", ""ItemDurability""
FROM ""Equipment"" WHERE ""Type"" = 2;

INSERT INTO ""ServiceAssets"" (""Id"", ""AssistsSkillName"", ""DiceBonusMin"", ""DiceBonusMax"", ""RecurringResourcesCost"")
SELECT ""Id"", ""AssistsSkillName"", ""DiceBonusMin"", ""DiceBonusMax"", ""RecurringResourcesCost""
FROM ""Equipment"" WHERE ""Type"" = 3;

INSERT INTO ""CharacterAssets"" (""Id"", ""CharacterId"", ""AssetId"", ""Quantity"", ""IsEquipped"", ""ReadySlotIndex"", ""CurrentStructure"", ""Notes"")
OVERRIDING SYSTEM VALUE
SELECT ""Id"", ""CharacterId"", ""EquipmentId"", ""Quantity"", ""IsEquipped"", ""ReadySlotIndex"", ""CurrentStructure"", ""Notes""
FROM ""CharacterEquipments"";

SELECT setval(pg_get_serial_sequence('""Assets""', 'Id'), (SELECT COALESCE(MAX(""Id""), 1) FROM ""Assets""));
SELECT setval(pg_get_serial_sequence('""CharacterAssets""', 'Id'), (SELECT COALESCE(MAX(""Id""), 1) FROM ""CharacterAssets""));
SELECT setval(pg_get_serial_sequence('""AssetCapabilities""', 'Id'), (SELECT COALESCE(MAX(""Id""), 1) FROM ""AssetCapabilities""));
SELECT setval(pg_get_serial_sequence('""PendingAssetProcurements""', 'Id'), (SELECT COALESCE(MAX(""Id""), 1) FROM ""PendingAssetProcurements""));
");

            migrationBuilder.DropTable(name: "CharacterEquipments");
            migrationBuilder.DropTable(name: "Equipment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArmorAssets");

            migrationBuilder.DropTable(
                name: "AssetCapabilities");

            migrationBuilder.DropTable(
                name: "CharacterAssets");

            migrationBuilder.DropTable(
                name: "EquipmentAssets");

            migrationBuilder.DropTable(
                name: "PendingAssetProcurements");

            migrationBuilder.DropTable(
                name: "ServiceAssets");

            migrationBuilder.DropTable(
                name: "WeaponAssets");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ArmorBallisticRating = table.Column<int>(type: "integer", nullable: false),
                    ArmorCoverage = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ArmorDefenseModifier = table.Column<int>(type: "integer", nullable: false),
                    ArmorEra = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ArmorIsConcealable = table.Column<bool>(type: "boolean", nullable: false),
                    ArmorRating = table.Column<int>(type: "integer", nullable: false),
                    ArmorSpeedModifier = table.Column<int>(type: "integer", nullable: false),
                    AssistsSkillName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Availability = table.Column<int>(type: "integer", nullable: false),
                    ClipInfo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Cost = table.Column<int>(type: "integer", nullable: false),
                    Damage = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DiceBonusMax = table.Column<int>(type: "integer", nullable: true),
                    DiceBonusMin = table.Column<int>(type: "integer", nullable: true),
                    InitiativeModifier = table.Column<int>(type: "integer", nullable: true),
                    IsIllicit = table.Column<bool>(type: "boolean", nullable: false),
                    IsRangedWeapon = table.Column<bool>(type: "boolean", nullable: false),
                    ItemCategory = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ItemDurability = table.Column<int>(type: "integer", nullable: true),
                    ItemSize = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Penalty = table.Column<int>(type: "integer", nullable: false),
                    Ranges = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RecurringResourcesCost = table.Column<int>(type: "integer", nullable: true),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    StrengthRequirement = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UsesBrawlForAttacks = table.Column<bool>(type: "boolean", nullable: false),
                    WeaponSpecialNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Weight = table.Column<float>(type: "real", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterEquipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    CurrentStructure = table.Column<int>(type: "integer", nullable: true),
                    IsEquipped = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReadySlotIndex = table.Column<int>(type: "integer", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterEquipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterEquipments_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterEquipments_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEquipments_CharacterId",
                table: "CharacterEquipments",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEquipments_EquipmentId",
                table: "CharacterEquipments",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Slug",
                table: "Equipment",
                column: "Slug");
        }
    }
}
