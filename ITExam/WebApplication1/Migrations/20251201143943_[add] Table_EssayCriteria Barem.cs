using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITExam.Migrations
{
    /// <inheritdoc />
    public partial class addTable_EssayCriteriaBarem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuestionScore",
                table: "QuestionBank");

            migrationBuilder.CreateTable(
                name: "EssayCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Percent = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EssayCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EssayCriteria_QuestionBank_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QuestionBank",
                        principalColumn: "QuestionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EssayCriteria_QuestionId",
                table: "EssayCriteria",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EssayCriteria");

            migrationBuilder.AddColumn<double>(
                name: "QuestionScore",
                table: "QuestionBank",
                type: "float",
                nullable: true);
        }
    }
}
