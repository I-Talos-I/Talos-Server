using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Talos.Server.Migrations
{
    /// <inheritdoc />
    public partial class Initial2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Templates_Users_UserId1",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_UserId1",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Templates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "Templates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Templates_UserId1",
                table: "Templates",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_Users_UserId1",
                table: "Templates",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
