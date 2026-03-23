using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase11BackpackWeaponSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WeaponSlotPoints",
                table: "WeaponAssets",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "BackpackSlotIndex",
                table: "CharacterAssets",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAssets_CharacterId_BackpackSlotIndex",
                table: "CharacterAssets",
                columns: new[] { "CharacterId", "BackpackSlotIndex" },
                unique: true,
                filter: "\"BackpackSlotIndex\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CharacterAssets_CharacterId_BackpackSlotIndex",
                table: "CharacterAssets");

            migrationBuilder.DropColumn(
                name: "WeaponSlotPoints",
                table: "WeaponAssets");

            migrationBuilder.DropColumn(
                name: "BackpackSlotIndex",
                table: "CharacterAssets");
        }
    }
}
