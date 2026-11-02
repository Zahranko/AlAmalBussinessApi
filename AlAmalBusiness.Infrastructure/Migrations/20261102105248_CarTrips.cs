using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CarTrips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarTrips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarId = table.Column<int>(type: "int", nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Destination = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartOdometer = table.Column<int>(type: "int", nullable: true),
                    EndOdometer = table.Column<int>(type: "int", nullable: true),
                    DepartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastEditedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LastEditedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarTrips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarTrips_AspNetUsers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CarTrips_AspNetUsers_LastEditedById",
                        column: x => x.LastEditedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CarTrips_SecurityCars_CarId",
                        column: x => x.CarId,
                        principalTable: "SecurityCars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarTrips_CarId_DepartedAt",
                table: "CarTrips",
                columns: new[] { "CarId", "DepartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CarTrips_CreatedAt",
                table: "CarTrips",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CarTrips_DriverId_CreatedAt",
                table: "CarTrips",
                columns: new[] { "DriverId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CarTrips_LastEditedById",
                table: "CarTrips",
                column: "LastEditedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarTrips");
        }
    }
}
