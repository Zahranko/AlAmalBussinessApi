using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AppointmentsByDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentRequests_AppointmentProcedures_ProcedureId",
                table: "AppointmentRequests");

            migrationBuilder.DropTable(
                name: "AppointmentNotificationEmails");

            migrationBuilder.DropTable(
                name: "AppointmentProcedures");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_ProcedureId",
                table: "AppointmentRequests");

            // NOT a rename of ProcedureId: its values are procedure ids, and
            // reusing them as AppointmentStatus would read every existing
            // request back as a random status. The column goes, and Status
            // starts every row at New (0).
            migrationBuilder.DropColumn(
                name: "ProcedureId",
                table: "AppointmentRequests");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AppointmentRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AssignedToId",
                table: "AppointmentRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "AppointmentRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "AppointmentRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Requests that arrived before this migration have no department
            // — they were routed by an email list instead. Park them on the
            // first department there is so the FK below can be created; an
            // admin can forward them where they belong from the inbox. If
            // there are no departments at all there are no requests either
            // (the public page couldn't have shown a picker), so the UPDATE
            // simply matches nothing.
            migrationBuilder.Sql(@"
UPDATE AppointmentRequests
SET DepartmentId = (SELECT MIN(Id) FROM Departments)
WHERE DepartmentId = 0 AND EXISTS (SELECT 1 FROM Departments);");

            migrationBuilder.CreateTable(
                name: "AppointmentHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: true),
                    FromDepartmentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ToDepartmentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentHistories_AppointmentRequests_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "AppointmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentHistories_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_AssignedToId",
                table: "AppointmentRequests",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_DepartmentId_CreatedDate",
                table: "AppointmentRequests",
                columns: new[] { "DepartmentId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_Status_CreatedDate",
                table: "AppointmentRequests",
                columns: new[] { "Status", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentHistories_ActorId",
                table: "AppointmentHistories",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentHistories_AppointmentId_CreatedAt",
                table: "AppointmentHistories",
                columns: new[] { "AppointmentId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentRequests_AspNetUsers_AssignedToId",
                table: "AppointmentRequests",
                column: "AssignedToId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentRequests_Departments_DepartmentId",
                table: "AppointmentRequests",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentRequests_AspNetUsers_AssignedToId",
                table: "AppointmentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentRequests_Departments_DepartmentId",
                table: "AppointmentRequests");

            migrationBuilder.DropTable(
                name: "AppointmentHistories");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_AssignedToId",
                table: "AppointmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_DepartmentId_CreatedDate",
                table: "AppointmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_Status_CreatedDate",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AppointmentRequests");

            // The old procedure ids are not recoverable — the list they
            // pointed at is re-created empty below.
            migrationBuilder.AddColumn<int>(
                name: "ProcedureId",
                table: "AppointmentRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AppointmentNotificationEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
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
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentProcedures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_ProcedureId",
                table: "AppointmentRequests",
                column: "ProcedureId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentRequests_AppointmentProcedures_ProcedureId",
                table: "AppointmentRequests",
                column: "ProcedureId",
                principalTable: "AppointmentProcedures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
