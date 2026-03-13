using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFidoCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FidoStoredCredentials",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    PublicKey = table.Column<byte[]>(nullable: false),
                    UserHandle = table.Column<byte[]>(nullable: false),
                    SignatureCounter = table.Column<long>(nullable: false),
                    CredId = table.Column<string>(nullable: false),
                    RegDate = table.Column<DateTime>(nullable: false),
                    AaGuid = table.Column<Guid>(nullable: false),
                    CredType = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FidoStoredCredentials", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_FidoStoredCredentials_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FidoStoredCredentials");
        }
    }
}
