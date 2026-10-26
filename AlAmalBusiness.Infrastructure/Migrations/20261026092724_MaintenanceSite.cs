using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MaintenanceSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Site",
                table: "MaintenanceTickets",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Site",
                table: "MaintenanceTickets");
        }
    }
}
