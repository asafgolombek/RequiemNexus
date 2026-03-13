using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNpcStatBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NpcStatBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CampaignId = table.Column<int>(nullable: true),
                    Name = table.Column<string>(nullable: false),
                    Concept = table.Column<string>(nullable: false),
                    Size = table.Column<int>(nullable: false),
                    Health = table.Column<int>(nullable: false),
                    Willpower = table.Column<int>(nullable: false),
                    BludgeoningArmor = table.Column<int>(nullable: false),
                    LethalArmor = table.Column<int>(nullable: false),
                    AttributesJson = table.Column<string>(nullable: false),
                    SkillsJson = table.Column<string>(nullable: false),
                    DisciplinesJson = table.Column<string>(nullable: false),
                    Notes = table.Column<string>(nullable: false),
                    IsPrebuilt = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcStatBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NpcStatBlocks_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NpcStatBlocks_CampaignId",
                table: "NpcStatBlocks",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_NpcStatBlocks_IsPrebuilt",
                table: "NpcStatBlocks",
                column: "IsPrebuilt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NpcStatBlocks");
        }
    }
}
