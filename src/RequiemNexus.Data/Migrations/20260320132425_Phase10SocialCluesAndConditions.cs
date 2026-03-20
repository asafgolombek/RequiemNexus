using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase10SocialCluesAndConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvestigationProgressTowardNextClue",
                table: "SocialManeuvers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SocialManeuverInvestigationSuccessesPerClue",
                table: "Campaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvestigationProgressTowardNextClue",
                table: "SocialManeuvers");

            migrationBuilder.DropColumn(
                name: "SocialManeuverInvestigationSuccessesPerClue",
                table: "Campaigns");
        }
    }
}
