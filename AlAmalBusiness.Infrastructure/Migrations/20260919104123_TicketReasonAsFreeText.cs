using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TicketReasonAsFreeText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The reason stops being a pick from a list and becomes the
            // raiser's own text. Add the column first and carry every picked
            // reason's name into it, so no ticket loses what it said before the
            // list and its foreign key go.
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Tickets",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE t SET t.Reason = r.Name
FROM Tickets t
INNER JOIN TicketReasons r ON r.Id = t.ReasonId;");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_TicketReasons_ReasonId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ReasonId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ReasonId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "TicketReasons");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The list comes back empty: free-text reasons can't be turned
            // back into list entries, so Reason is dropped with its text.
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Tickets");

            migrationBuilder.AddColumn<int>(
                name: "ReasonId",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TicketReasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcedureId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketReasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketReasons_TicketProcedures_ProcedureId",
                        column: x => x.ProcedureId,
                        principalTable: "TicketProcedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ReasonId",
                table: "Tickets",
                column: "ReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketReasons_ProcedureId_Name",
                table: "TicketReasons",
                columns: new[] { "ProcedureId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_TicketReasons_ReasonId",
                table: "Tickets",
                column: "ReasonId",
                principalTable: "TicketReasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
