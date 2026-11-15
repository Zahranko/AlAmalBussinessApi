using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentPublicVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every existing department stays on both public pages: true for
            // the rows already there, so nothing disappears on deploy.
            migrationBuilder.AddColumn<bool>(
                name: "ShowOnAppointment",
                table: "Departments",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnFeedback",
                table: "Departments",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowOnAppointment",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "ShowOnFeedback",
                table: "Departments");
        }
    }
}
