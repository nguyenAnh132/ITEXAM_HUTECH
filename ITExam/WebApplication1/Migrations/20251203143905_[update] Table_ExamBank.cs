using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITExam.Migrations
{
    /// <inheritdoc />
    public partial class updateTable_ExamBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubjectName",
                table: "ExamBank",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubjectName",
                table: "ExamBank");
        }
    }
}
