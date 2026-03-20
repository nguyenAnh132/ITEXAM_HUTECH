using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITExam.Migrations
{
    /// <inheritdoc />
    public partial class initialdb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subject",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subject", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ClassName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Faculty = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "SubjectChapter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectChapter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectChapter_Subject_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Class",
                columns: table => new
                {
                    ClassId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ClassCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Class", x => x.ClassId);
                    table.ForeignKey(
                        name: "FK_Class_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ExamBank",
                columns: table => new
                {
                    ExamBankId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamBankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    SubjectName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ExamType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamBank", x => x.ExamBankId);
                    table.ForeignKey(
                        name: "FK_ExamBank_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "SubjectClo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChapterId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectClo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectClo_SubjectChapter_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "SubjectChapter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassDetail",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassDetail", x => new { x.UserId, x.ClassId });
                    table.ForeignKey(
                        name: "FK_ClassDetail_Class_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Class",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassDetail_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Exam",
                columns: table => new
                {
                    ExamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    ExamType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExamBankId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exam", x => x.ExamId);
                    table.ForeignKey(
                        name: "FK_Exam_ExamBank_ExamBankId",
                        column: x => x.ExamBankId,
                        principalTable: "ExamBank",
                        principalColumn: "ExamBankId");
                    table.ForeignKey(
                        name: "FK_Exam_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionBank",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChoiceContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CLOId = table.Column<int>(type: "int", nullable: true),
                    ChapterId = table.Column<int>(type: "int", nullable: true),
                    QuestionType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExamBankId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionBank", x => x.QuestionId);
                    table.ForeignKey(
                        name: "FK_QuestionBank_ExamBank_ExamBankId",
                        column: x => x.ExamBankId,
                        principalTable: "ExamBank",
                        principalColumn: "ExamBankId");
                });

            migrationBuilder.CreateTable(
                name: "ActivityLog",
                columns: table => new
                {
                    ActivityLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    InstructorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLog", x => x.ActivityLogId);
                    table.ForeignKey(
                        name: "FK_ActivityLog_Class_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Class",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityLog_Exam_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exam",
                        principalColumn: "ExamId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityLog_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassExam",
                columns: table => new
                {
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    IsExam = table.Column<bool>(type: "bit", nullable: false),
                    Access = table.Column<bool>(type: "bit", nullable: true),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassExam", x => new { x.ClassId, x.ExamId });
                    table.ForeignKey(
                        name: "FK_ClassExam_Class_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Class",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassExam_Exam_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exam",
                        principalColumn: "ExamId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamHistory",
                columns: table => new
                {
                    ExamHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HangfireJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SubmitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: true),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    UploadedFileId = table.Column<string>(type: "varchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamHistory", x => x.ExamHistoryId);
                    table.ForeignKey(
                        name: "FK_ExamHistory_Class_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Class",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamHistory_Exam_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exam",
                        principalColumn: "ExamId");
                    table.ForeignKey(
                        name: "FK_ExamHistory_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ExamMatrix",
                columns: table => new
                {
                    ExamMatrixId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    CLOId = table.Column<int>(type: "int", nullable: false),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
                    CloScore = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamMatrix", x => x.ExamMatrixId);
                    table.ForeignKey(
                        name: "FK_ExamMatrix_Exam_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exam",
                        principalColumn: "ExamId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EssayCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Percent = table.Column<int>(type: "int", nullable: false)
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
                name: "StudentAnswer",
                columns: table => new
                {
                    StudentAnswerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    ExamHistoryId = table.Column<int>(type: "int", nullable: false),
                    EssayAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MultipleChoiceAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Score = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAnswer", x => x.StudentAnswerId);
                    table.ForeignKey(
                        name: "FK_StudentAnswer_ExamHistory_ExamHistoryId",
                        column: x => x.ExamHistoryId,
                        principalTable: "ExamHistory",
                        principalColumn: "ExamHistoryId");
                    table.ForeignKey(
                        name: "FK_StudentAnswer_QuestionBank_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QuestionBank",
                        principalColumn: "QuestionId");
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
                name: "IX_ActivityLog_ClassId",
                table: "ActivityLog",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLog_ExamId",
                table: "ActivityLog",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLog_UserId",
                table: "ActivityLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Class_UserId",
                table: "Class",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassDetail_ClassId",
                table: "ClassDetail",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassExam_ExamId",
                table: "ClassExam",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_EssayCriteria_QuestionId",
                table: "EssayCriteria",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_EssayGradingDetail_GradingResultId",
                table: "EssayGradingDetail",
                column: "GradingResultId");

            migrationBuilder.CreateIndex(
                name: "IX_EssayGradingResult_ExamHistoryId",
                table: "EssayGradingResult",
                column: "ExamHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Exam_ExamBankId",
                table: "Exam",
                column: "ExamBankId");

            migrationBuilder.CreateIndex(
                name: "IX_Exam_UserId",
                table: "Exam",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamBank_UserId",
                table: "ExamBank",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamHistory_ClassId",
                table: "ExamHistory",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamHistory_ExamId",
                table: "ExamHistory",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamHistory_UserId",
                table: "ExamHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMatrix_ExamId",
                table: "ExamMatrix",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_ExamBankId",
                table: "QuestionBank",
                column: "ExamBankId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_ExamHistoryId",
                table: "StudentAnswer",
                column: "ExamHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_QuestionId",
                table: "StudentAnswer",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectChapter_SubjectId",
                table: "SubjectChapter",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectClo_ChapterId",
                table: "SubjectClo",
                column: "ChapterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLog");

            migrationBuilder.DropTable(
                name: "ClassDetail");

            migrationBuilder.DropTable(
                name: "ClassExam");

            migrationBuilder.DropTable(
                name: "EssayCriteria");

            migrationBuilder.DropTable(
                name: "EssayGradingDetail");

            migrationBuilder.DropTable(
                name: "ExamMatrix");

            migrationBuilder.DropTable(
                name: "StudentAnswer");

            migrationBuilder.DropTable(
                name: "SubjectClo");

            migrationBuilder.DropTable(
                name: "EssayGradingResult");

            migrationBuilder.DropTable(
                name: "QuestionBank");

            migrationBuilder.DropTable(
                name: "SubjectChapter");

            migrationBuilder.DropTable(
                name: "ExamHistory");

            migrationBuilder.DropTable(
                name: "Subject");

            migrationBuilder.DropTable(
                name: "Class");

            migrationBuilder.DropTable(
                name: "Exam");

            migrationBuilder.DropTable(
                name: "ExamBank");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
