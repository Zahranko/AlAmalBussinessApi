using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MammogramAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MammogramAppointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneCountryCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AppointmentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Room = table.Column<int>(type: "int", nullable: false),
                    Procedures = table.Column<int>(type: "int", nullable: false),
                    ReferringDoctor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReceivingDoctor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArrivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MammogramAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MammogramAppointments_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MammogramAppointmentHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ResultingStatus = table.Column<int>(type: "int", nullable: true),
                    AppointmentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Room = table.Column<int>(type: "int", nullable: true),
                    CallSucceeded = table.Column<bool>(type: "bit", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MammogramAppointmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MammogramAppointmentHistories_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MammogramAppointmentHistories_MammogramAppointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "MammogramAppointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MammogramAppointmentHistories_ActorId",
                table: "MammogramAppointmentHistories",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_MammogramAppointmentHistories_AppointmentId_CreatedAt",
                table: "MammogramAppointmentHistories",
                columns: new[] { "AppointmentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MammogramAppointments_AppointmentAt_Room",
                table: "MammogramAppointments",
                columns: new[] { "AppointmentAt", "Room" });

            migrationBuilder.CreateIndex(
                name: "IX_MammogramAppointments_CreatedAt",
                table: "MammogramAppointments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MammogramAppointments_CreatedById",
                table: "MammogramAppointments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MammogramAppointments_Status_AppointmentAt",
                table: "MammogramAppointments",
                columns: new[] { "Status", "AppointmentAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MammogramAppointmentHistories");

            migrationBuilder.DropTable(
                name: "MammogramAppointments");
        }
    }
}
