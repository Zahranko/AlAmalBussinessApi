using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TextQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "QuestionnaireQuestions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "QuestionnaireQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Rating",
                table: "QuestionnaireAnswers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Text",
                table: "QuestionnaireAnswers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            // Every question that existed before this migration is a rating
            // question (Type defaults to 0 = Rating), and a rating question
            // is always required — the bit column's default of 0 would say
            // the opposite to anyone reading the table. The code never asks
            // IsRequired about a rating question, so this is for the data's
            // own honesty rather than for behaviour.
            // Wrapped in EXEC on purpose: `dotnet ef database update` runs
            // each operation as its own command, but `migrations script`
            // emits the whole migration as ONE batch, and SQL Server
            // compiles a batch before running it — a plain UPDATE naming
            // columns added a few lines above fails with "Invalid column
            // name" before anything executes. EXEC defers the compile.
            migrationBuilder.Sql("EXEC(N'UPDATE [QuestionnaireQuestions] SET [IsRequired] = 1 WHERE [Type] = 0;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "QuestionnaireQuestions");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "QuestionnaireQuestions");

            migrationBuilder.DropColumn(
                name: "Text",
                table: "QuestionnaireAnswers");

            migrationBuilder.AlterColumn<int>(
                name: "Rating",
                table: "QuestionnaireAnswers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
