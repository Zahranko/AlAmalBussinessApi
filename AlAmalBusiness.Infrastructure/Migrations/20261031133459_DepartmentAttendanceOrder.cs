using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentAttendanceOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttendanceOrder",
                table: "Departments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Start from the public form's order so every row has a distinct
            // position; the admin rearranges it from Settings -> Attendance.
            // EXEC: the column doesn't exist when an idempotent script compiles.
            migrationBuilder.Sql("EXEC('UPDATE Departments SET AttendanceOrder = DisplayOrder')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendanceOrder",
                table: "Departments");
        }
    }
}
