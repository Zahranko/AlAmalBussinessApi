using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Departments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill alphabetically, which is exactly the order the public
            // feedback form listed departments in before this column existed
            // — so nothing visibly moves until an admin reorders them.
            // Wrapped in EXEC so the UPDATE compiles after the column above
            // exists: `dotnet ef migrations script` emits both into a single
            // batch, and SQL Server would otherwise fail the whole batch with
            // "Invalid column name 'DisplayOrder'" before running any of it.
            migrationBuilder.Sql(@"
                EXEC(N'
                    WITH ordered AS (
                        SELECT DisplayOrder,
                               ROW_NUMBER() OVER (ORDER BY Name) AS NewOrder
                        FROM Departments
                    )
                    UPDATE ordered SET DisplayOrder = NewOrder;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Departments");
        }
    }
}
