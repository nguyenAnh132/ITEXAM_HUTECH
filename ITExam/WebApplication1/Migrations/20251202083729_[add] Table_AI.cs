using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITExam.Migrations
{
    /// <inheritdoc />
    public partial class addTable_AI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EssayGradingResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamHistoryId = table.Column<int>(type: "int", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverallComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EssayGradingResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EssayGradingResult_ExamHistory_ExamHistoryId",
                        column: x => x.ExamHistoryId,
                        principalTable: "ExamHistory",
                        principalColumn: "ExamHistoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EssayGradingDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GradingResultId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EssayGradingDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EssayGradingDetail_EssayGradingResult_GradingResultId",
                        column: x => x.GradingResultId,
                        principalTable: "EssayGradingResult",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EssayGradingDetail_GradingResultId",
                table: "EssayGradingDetail",
                column: "GradingResultId");

            migrationBuilder.CreateIndex(
                name: "IX_EssayGradingResult_ExamHistoryId",
                table: "EssayGradingResult",
                column: "ExamHistoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EssayGradingDetail");

            migrationBuilder.DropTable(
                name: "EssayGradingResult");
        }
    }
}
