using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadListIndexesAndLengths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeadCalls_LeadId",
                table: "LeadCalls");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNum",
                table: "Leads",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NickName",
                table: "Leads",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Leads",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CountryKey",
                table: "Leads",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leads_CreatedDate",
                table: "Leads",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Status_CreatedDate",
                table: "Leads",
                columns: new[] { "Status", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadHistories_Type_ResultingStatus",
                table: "LeadHistories",
                columns: new[] { "Type", "ResultingStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadCalls_LeadId_CreatedAt",
                table: "LeadCalls",
                columns: new[] { "LeadId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leads_CreatedDate",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_Status_CreatedDate",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_LeadHistories_Type_ResultingStatus",
                table: "LeadHistories");

            migrationBuilder.DropIndex(
                name: "IX_LeadCalls_LeadId_CreatedAt",
                table: "LeadCalls");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNum",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NickName",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CountryKey",
                table: "Leads",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeadCalls_LeadId",
                table: "LeadCalls",
                column: "LeadId");
        }
    }
}
