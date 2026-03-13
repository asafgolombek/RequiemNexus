using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferencesAndConsentLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnAccountChanges",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnNewsletter",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnSecurityEvents",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ConsentLogs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(nullable: false),
                    ConsentedAt = table.Column<DateTimeOffset>(nullable: false),
                    DocumentVersion = table.Column<string>(maxLength: 20, nullable: false),
                    IpAddress = table.Column<string>(maxLength: 45, nullable: true),
                    ConsentType = table.Column<string>(maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsentLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentLogs_UserId",
                table: "ConsentLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsentLogs");

            migrationBuilder.DropColumn(
                name: "NotifyOnAccountChanges",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NotifyOnNewsletter",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NotifyOnSecurityEvents",
                table: "AspNetUsers");
        }
    }
}
