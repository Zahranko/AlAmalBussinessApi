using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CarChecksAndClosedMonths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "AttendanceSheets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedById",
                table: "AttendanceSheets",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "AttendanceSheets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SecurityCars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlateNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityCars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarCheckSheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedById = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarCheckSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarCheckSheets_AspNetUsers_ClosedById",
                        column: x => x.ClosedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CarCheckSheets_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CarCheckSheets_SecurityCars_CarId",
                        column: x => x.CarId,
                        principalTable: "SecurityCars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CarCheckDays",
                columns: table => new
                {
                    SheetId = table.Column<int>(type: "int", nullable: false),
                    Day = table.Column<int>(type: "int", nullable: false),
                    EngineOil = table.Column<bool>(type: "bit", nullable: true),
                    BrakeOil = table.Column<bool>(type: "bit", nullable: true),
                    PowerSteeringOil = table.Column<bool>(type: "bit", nullable: true),
                    RadiatorWater = table.Column<bool>(type: "bit", nullable: true),
                    BodyDamage = table.Column<bool>(type: "bit", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmployeeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarCheckDays", x => new { x.SheetId, x.Day });
                    table.ForeignKey(
                        name: "FK_CarCheckDays_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CarCheckDays_CarCheckSheets_SheetId",
                        column: x => x.SheetId,
                        principalTable: "CarCheckSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSheets_ClosedById",
                table: "AttendanceSheets",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_CarCheckDays_UpdatedById",
                table: "CarCheckDays",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CarCheckSheets_CarId_Year_Month",
                table: "CarCheckSheets",
                columns: new[] { "CarId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarCheckSheets_ClosedById",
                table: "CarCheckSheets",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_CarCheckSheets_CreatedById",
                table: "CarCheckSheets",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityCars_Name",
                table: "SecurityCars",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSheets_AspNetUsers_ClosedById",
                table: "AttendanceSheets",
                column: "ClosedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSheets_AspNetUsers_ClosedById",
                table: "AttendanceSheets");

            migrationBuilder.DropTable(
                name: "CarCheckDays");

            migrationBuilder.DropTable(
                name: "CarCheckSheets");

            migrationBuilder.DropTable(
                name: "SecurityCars");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceSheets_ClosedById",
                table: "AttendanceSheets");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "AttendanceSheets");

            migrationBuilder.DropColumn(
                name: "ClosedById",
                table: "AttendanceSheets");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "AttendanceSheets");
        }
    }
}
