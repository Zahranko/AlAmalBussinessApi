using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MammogramSecondFollowUpAndReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FollowUpDate2",
                table: "MammogramRecords",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpExam2",
                table: "MammogramRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reminder2Error",
                table: "MammogramRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reminder2MessageId",
                table: "MammogramRecords",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Reminder2SentAt",
                table: "MammogramRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Reminder2Status",
                table: "MammogramRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MammogramReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MammogramReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MammogramReports_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MammogramRecords_FollowUpDate2_Reminder2Status",
                table: "MammogramRecords",
                columns: new[] { "FollowUpDate2", "Reminder2Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MammogramReports_CreatedById",
                table: "MammogramReports",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MammogramReports_Name",
                table: "MammogramReports",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MammogramReports");

            migrationBuilder.DropIndex(
                name: "IX_MammogramRecords_FollowUpDate2_Reminder2Status",
                table: "MammogramRecords");

            migrationBuilder.DropColumn(
                name: "FollowUpDate2",
                table: "MammogramRecords");

            migrationBuilder.DropColumn(
                name: "FollowUpExam2",
                table: "MammogramRecords");

            migrationBuilder.DropColumn(
                name: "Reminder2Error",
                table: "MammogramRecords");

            migrationBuilder.DropColumn(
                name: "Reminder2MessageId",
                table: "MammogramRecords");

            migrationBuilder.DropColumn(
                name: "Reminder2SentAt",
                table: "MammogramRecords");

            migrationBuilder.DropColumn(
                name: "Reminder2Status",
                table: "MammogramRecords");
        }
    }
}
