using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeerTap.Migrations
{
    /// <inheritdoc />
    public partial class AddTapToTapSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TapId",
                table: "TapSessions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "Taps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Taps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TapSessions_TapId",
                table: "TapSessions",
                column: "TapId");

            migrationBuilder.AddForeignKey(
                name: "FK_TapSessions_Taps_TapId",
                table: "TapSessions",
                column: "TapId",
                principalTable: "Taps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TapSessions_Taps_TapId",
                table: "TapSessions");

            migrationBuilder.DropTable(
                name: "Taps");

            migrationBuilder.DropIndex(
                name: "IX_TapSessions_TapId",
                table: "TapSessions");

            migrationBuilder.AlterColumn<string>(
                name: "TapId",
                table: "TapSessions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }
    }
}
