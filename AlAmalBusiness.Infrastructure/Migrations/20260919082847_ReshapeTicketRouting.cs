using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReshapeTicketRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_DepartmentId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_PaymentMethod_Status_CreatedDate",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Tickets");

            migrationBuilder.AddColumn<bool>(
                name: "IsInsurance",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsInsurance",
                table: "TicketProcedures",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_DepartmentId_CreatedDate",
                table: "Tickets",
                columns: new[] { "DepartmentId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_IsInsurance_Status_CreatedDate",
                table: "Tickets",
                columns: new[] { "IsInsurance", "Status", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_DepartmentId_CreatedDate",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_IsInsurance_Status_CreatedDate",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "IsInsurance",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AllowsInsurance",
                table: "TicketProcedures");

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_DepartmentId",
                table: "Tickets",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_PaymentMethod_Status_CreatedDate",
                table: "Tickets",
                columns: new[] { "PaymentMethod", "Status", "CreatedDate" });
        }
    }
}
