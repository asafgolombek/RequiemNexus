#pragma warning disable S1192 // String literals should not be duplicated
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaign : Migration
    {
        private const string TableCampaigns = "Campaigns";
        private const string TableCharacters = "Characters";
        private const string ColumnCampaignId = "CampaignId";
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: ColumnCampaignId,
                table: TableCharacters,
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: TableCampaigns,
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StoryTellerId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campaigns_AspNetUsers_StoryTellerId",
                        column: x => x.StoryTellerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_CampaignId",
                table: TableCharacters,
                column: ColumnCampaignId);

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_StoryTellerId",
                table: TableCampaigns,
                column: "StoryTellerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Campaigns_CampaignId",
                table: TableCharacters,
                column: ColumnCampaignId,
                principalTable: TableCampaigns,
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Campaigns_CampaignId",
                table: TableCharacters);

            migrationBuilder.DropTable(
                name: TableCampaigns);

            migrationBuilder.DropIndex(
                name: "IX_Characters_CampaignId",
                table: TableCharacters);

            migrationBuilder.DropColumn(
                name: ColumnCampaignId,
                table: TableCharacters);
        }
    }
}

