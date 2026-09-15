using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentNotificationEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentNotificationEmails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentProcedures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentProcedures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentReferralSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentReferralSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PhoneCountryCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProcedureId = table.Column<int>(type: "int", nullable: false),
                    ReferralSourceId = table.Column<int>(type: "int", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SubmittedFromIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentRequests_AppointmentProcedures_ProcedureId",
                        column: x => x.ProcedureId,
                        principalTable: "AppointmentProcedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentRequests_AppointmentReferralSources_ReferralSourceId",
                        column: x => x.ReferralSourceId,
                        principalTable: "AppointmentReferralSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentNotificationEmails_Email",
                table: "AppointmentNotificationEmails",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentProcedures_Name",
                table: "AppointmentProcedures",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentReferralSources_Name",
                table: "AppointmentReferralSources",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_CreatedDate",
                table: "AppointmentRequests",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_ProcedureId",
                table: "AppointmentRequests",
                column: "ProcedureId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_ReferralSourceId",
                table: "AppointmentRequests",
                column: "ReferralSourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentNotificationEmails");

            migrationBuilder.DropTable(
                name: "AppointmentRequests");

            migrationBuilder.DropTable(
                name: "AppointmentProcedures");

            migrationBuilder.DropTable(
                name: "AppointmentReferralSources");
        }
    }
}
