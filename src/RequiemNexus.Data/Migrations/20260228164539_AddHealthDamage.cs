using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthDamage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HealthDamage",
                table: "Characters",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HealthDamage",
                table: "Characters");
        }
    }
}
