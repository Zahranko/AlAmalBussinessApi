using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedLeads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Leads",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DeletedLeads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletedLeads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeletedLeads_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeletedLeads_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeletedLeads_DeletedAt",
                table: "DeletedLeads",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeletedLeads_DeletedById",
                table: "DeletedLeads",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_DeletedLeads_LeadId",
                table: "DeletedLeads",
                column: "LeadId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeletedLeads");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Leads");
        }
    }
}
