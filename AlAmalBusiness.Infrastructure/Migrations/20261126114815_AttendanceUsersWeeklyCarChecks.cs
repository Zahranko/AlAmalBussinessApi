using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceUsersWeeklyCarChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttendanceRows_SheetId_EmployeeNumber",
                table: "AttendanceRows");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "AttendanceRows",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNumber",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRows_SheetId_UserId",
                table: "AttendanceRows",
                columns: new[] { "SheetId", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRows_UserId",
                table: "AttendanceRows",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EmployeeNumber",
                table: "AspNetUsers",
                column: "EmployeeNumber",
                unique: true,
                filter: "[EmployeeNumber] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRows_AspNetUsers_UserId",
                table: "AttendanceRows",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Car checks became weekly (days 1-7, 8-14, ...): keep only the
            // latest filled day of each week of each sheet.
            migrationBuilder.Sql(@"DELETE d FROM CarCheckDays d
WHERE EXISTS (SELECT 1 FROM CarCheckDays x
              WHERE x.SheetId = d.SheetId AND (x.Day - 1) / 7 = (d.Day - 1) / 7 AND x.Day > d.Day);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRows_AspNetUsers_UserId",
                table: "AttendanceRows");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRows_SheetId_UserId",
                table: "AttendanceRows");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRows_UserId",
                table: "AttendanceRows");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_EmployeeNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AttendanceRows");

            migrationBuilder.DropColumn(
                name: "EmployeeNumber",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRows_SheetId_EmployeeNumber",
                table: "AttendanceRows",
                columns: new[] { "SheetId", "EmployeeNumber" },
                unique: true);
        }
    }
}
