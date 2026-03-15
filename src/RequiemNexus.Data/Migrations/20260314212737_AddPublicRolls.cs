using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicRolls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublicRolls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    CampaignId = table.Column<int>(type: "integer", nullable: true),
                    RolledByUserId = table.Column<string>(type: "text", nullable: false),
                    PoolDescription = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResultJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicRolls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicRolls_AspNetUsers_RolledByUserId",
                        column: x => x.RolledByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicRolls_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicRolls_CampaignId",
                table: "PublicRolls",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicRolls_RolledByUserId",
                table: "PublicRolls",
                column: "RolledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicRolls_Slug",
                table: "PublicRolls",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicRolls");
        }
    }
}
