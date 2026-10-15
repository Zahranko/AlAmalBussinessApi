using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMammograms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MammogramSheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MammogramSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MammogramSheets_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MammogramRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SheetId = table.Column<int>(type: "int", nullable: false),
                    PatientMrn = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tech = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Age = table.Column<int>(type: "int", nullable: true),
                    BreastFeeding = table.Column<bool>(type: "bit", nullable: false),
                    PossiblePregnancy = table.Column<bool>(type: "bit", nullable: false),
                    HasImplants = table.Column<bool>(type: "bit", nullable: false),
                    VisitDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Procedures = table.Column<int>(type: "int", nullable: false),
                    ExamType = table.Column<int>(type: "int", nullable: true),
                    BiRads = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FollowUpDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PhoneCountryCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ContactNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ContactedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReminderStatus = table.Column<int>(type: "int", nullable: false),
                    ReminderSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReminderMessageId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MammogramRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MammogramRecords_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MammogramRecords_MammogramSheets_SheetId",
                        column: x => x.SheetId,
                        principalTable: "MammogramSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MammogramRecords_CreatedById",
                table: "MammogramRecords",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MammogramRecords_FollowUpDate_ReminderStatus",
                table: "MammogramRecords",
                columns: new[] { "FollowUpDate", "ReminderStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_MammogramRecords_PatientMrn",
                table: "MammogramRecords",
                column: "PatientMrn");

            migrationBuilder.CreateIndex(
                name: "IX_MammogramRecords_SheetId_VisitDate",
                table: "MammogramRecords",
                columns: new[] { "SheetId", "VisitDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MammogramSheets_CreatedById",
                table: "MammogramSheets",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MammogramSheets_Year_Month",
                table: "MammogramSheets",
                columns: new[] { "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MammogramRecords");

            migrationBuilder.DropTable(
                name: "MammogramSheets");
        }
    }
}
