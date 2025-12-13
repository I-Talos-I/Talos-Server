using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Talos.Server.Migrations
{
    public partial class InitialBaseline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline migration – no schema changes
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback for baseline
        }
    }
}