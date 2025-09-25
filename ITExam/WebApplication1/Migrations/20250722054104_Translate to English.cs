using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITExam.Migrations
{
    /// <inheritdoc />
    public partial class TranslatetoEnglish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietBaiLams");

            migrationBuilder.DropTable(
                name: "ChiTietLopHocs");

            migrationBuilder.DropTable(
                name: "DanhSachDeThiCuaLopHocs");

            migrationBuilder.DropTable(
                name: "MaTranDeThis");

            migrationBuilder.DropTable(
                name: "NhatKyHoatDongs");

            migrationBuilder.DropTable(
                name: "CauHoiNganHangDes");

            migrationBuilder.DropTable(
                name: "LichSuLamBais");

            migrationBuilder.DropTable(
                name: "DeThis");

            migrationBuilder.DropTable(
                name: "LopHocs");

            migrationBuilder.DropTable(
                name: "NganHangDes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "User");

            migrationBuilder.RenameColumn(
                name: "VaiTro",
                table: "User",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "NgayTao",
                table: "User",
                newName: "UpdatedDate");

            migrationBuilder.RenameColumn(
                name: "NgayCapNhat",
                table: "User",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "Lop",
                table: "User",
                newName: "ClassName");

            migrationBuilder.RenameColumn(
                name: "Khoa",
                table: "User",
                newName: "Faculty");

            migrationBuilder.RenameColumn(
                name: "HoTen",
                table: "User",
                newName: "FullName");

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                table: "User",
                column: "UserId");

            migrationBuilder.CreateTable(
                name: "Class",
                columns: table => new
                {
                    ClassId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                    QuestionContent = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ChoiceContent = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CLOId = table.Column<int>(type: "int", nullable: true),
                    ChapterId = table.Column<int>(type: "int", nullable: true),
                    QuestionType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExamBankId = table.Column<int>(type: "int", nullable: false),
                    QuestionScore = table.Column<double>(type: "float", nullable: true)
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
                    SubmitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: true),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false)
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
                    QuestionCount = table.Column<int>(type: "int", nullable: false)
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
                name: "ExamMatrix");

            migrationBuilder.DropTable(
                name: "StudentAnswer");

            migrationBuilder.DropTable(
                name: "ExamHistory");

            migrationBuilder.DropTable(
                name: "QuestionBank");

            migrationBuilder.DropTable(
                name: "Class");

            migrationBuilder.DropTable(
                name: "Exam");

            migrationBuilder.DropTable(
                name: "ExamBank");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                table: "User");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "Users");

            migrationBuilder.RenameColumn(
                name: "UpdatedDate",
                table: "Users",
                newName: "NgayTao");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "Users",
                newName: "VaiTro");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Users",
                newName: "HoTen");

            migrationBuilder.RenameColumn(
                name: "Faculty",
                table: "Users",
                newName: "Khoa");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Users",
                newName: "NgayCapNhat");

            migrationBuilder.RenameColumn(
                name: "ClassName",
                table: "Users",
                newName: "Lop");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "UserId");

            migrationBuilder.CreateTable(
                name: "LopHocs",
                columns: table => new
                {
                    MaLopHoc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenLopHoc = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LopHocs", x => x.MaLopHoc);
                    table.ForeignKey(
                        name: "FK_LopHocs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "NganHangDes",
                columns: table => new
                {
                    MaNHD = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoaiDe = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaMonHoc = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenNHD = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NganHangDes", x => x.MaNHD);
                    table.ForeignKey(
                        name: "FK_NganHangDes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ChiTietLopHocs",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MaLopHoc = table.Column<int>(type: "int", nullable: false),
                    NgayThamGia = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietLopHocs", x => new { x.UserId, x.MaLopHoc });
                    table.ForeignKey(
                        name: "FK_ChiTietLopHocs_LopHocs_MaLopHoc",
                        column: x => x.MaLopHoc,
                        principalTable: "LopHocs",
                        principalColumn: "MaLopHoc",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietLopHocs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "CauHoiNganHangDes",
                columns: table => new
                {
                    MaCauHoi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNHD = table.Column<int>(type: "int", nullable: false),
                    DiemCauHoi = table.Column<double>(type: "float", nullable: true),
                    LoaiCauHoi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaCLO = table.Column<int>(type: "int", nullable: true),
                    MaChuong = table.Column<int>(type: "int", nullable: true),
                    NoiDungCauHoi = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    NoiDungLuaChon = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauHoiNganHangDes", x => x.MaCauHoi);
                    table.ForeignKey(
                        name: "FK_CauHoiNganHangDes_NganHangDes_MaNHD",
                        column: x => x.MaNHD,
                        principalTable: "NganHangDes",
                        principalColumn: "MaNHD");
                });

            migrationBuilder.CreateTable(
                name: "DeThis",
                columns: table => new
                {
                    MaDe = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNHD = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    LoaiDe = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ThoiGianLamBai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeThis", x => x.MaDe);
                    table.ForeignKey(
                        name: "FK_DeThis_NganHangDes_MaNHD",
                        column: x => x.MaNHD,
                        principalTable: "NganHangDes",
                        principalColumn: "MaNHD");
                    table.ForeignKey(
                        name: "FK_DeThis_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DanhSachDeThiCuaLopHocs",
                columns: table => new
                {
                    MaLopHoc = table.Column<int>(type: "int", nullable: false),
                    MaDe = table.Column<int>(type: "int", nullable: false),
                    Access = table.Column<bool>(type: "bit", nullable: true),
                    LaDeThi = table.Column<bool>(type: "bit", nullable: false),
                    NgayThem = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiGianBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiGianKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhSachDeThiCuaLopHocs", x => new { x.MaLopHoc, x.MaDe });
                    table.ForeignKey(
                        name: "FK_DanhSachDeThiCuaLopHocs_DeThis_MaDe",
                        column: x => x.MaDe,
                        principalTable: "DeThis",
                        principalColumn: "MaDe",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanhSachDeThiCuaLopHocs_LopHocs_MaLopHoc",
                        column: x => x.MaLopHoc,
                        principalTable: "LopHocs",
                        principalColumn: "MaLopHoc",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LichSuLamBais",
                columns: table => new
                {
                    LichSuId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDe = table.Column<int>(type: "int", nullable: false),
                    MaLopHoc = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Diem = table.Column<double>(type: "float", nullable: true),
                    ThoiGianBatDauLamBai = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiGianLamBai = table.Column<int>(type: "int", nullable: false),
                    ThoiGianNop = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuLamBais", x => x.LichSuId);
                    table.ForeignKey(
                        name: "FK_LichSuLamBais_DeThis_MaDe",
                        column: x => x.MaDe,
                        principalTable: "DeThis",
                        principalColumn: "MaDe");
                    table.ForeignKey(
                        name: "FK_LichSuLamBais_LopHocs_MaLopHoc",
                        column: x => x.MaLopHoc,
                        principalTable: "LopHocs",
                        principalColumn: "MaLopHoc",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LichSuLamBais_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "MaTranDeThis",
                columns: table => new
                {
                    MaMaTran = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDe = table.Column<int>(type: "int", nullable: false),
                    MaCLO = table.Column<int>(type: "int", nullable: false),
                    MaChuong = table.Column<int>(type: "int", nullable: false),
                    SoLuongCauHoi = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaTranDeThis", x => x.MaMaTran);
                    table.ForeignKey(
                        name: "FK_MaTranDeThis_DeThis_MaDe",
                        column: x => x.MaDe,
                        principalTable: "DeThis",
                        principalColumn: "MaDe",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyHoatDongs",
                columns: table => new
                {
                    MaNhatKy = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDe = table.Column<int>(type: "int", nullable: false),
                    MaLopHoc = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ChuoiGhiNhatKy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HoTenGV = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayGhi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyHoatDongs", x => x.MaNhatKy);
                    table.ForeignKey(
                        name: "FK_NhatKyHoatDongs_DeThis_MaDe",
                        column: x => x.MaDe,
                        principalTable: "DeThis",
                        principalColumn: "MaDe",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NhatKyHoatDongs_LopHocs_MaLopHoc",
                        column: x => x.MaLopHoc,
                        principalTable: "LopHocs",
                        principalColumn: "MaLopHoc",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NhatKyHoatDongs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietBaiLams",
                columns: table => new
                {
                    ChiTietBaiLamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LichSuId = table.Column<int>(type: "int", nullable: false),
                    MaCauHoi = table.Column<int>(type: "int", nullable: false),
                    CauTraLoiTN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CauTraLoiTuLuan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiemDuocCham = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietBaiLams", x => x.ChiTietBaiLamId);
                    table.ForeignKey(
                        name: "FK_ChiTietBaiLams_CauHoiNganHangDes_MaCauHoi",
                        column: x => x.MaCauHoi,
                        principalTable: "CauHoiNganHangDes",
                        principalColumn: "MaCauHoi");
                    table.ForeignKey(
                        name: "FK_ChiTietBaiLams_LichSuLamBais_LichSuId",
                        column: x => x.LichSuId,
                        principalTable: "LichSuLamBais",
                        principalColumn: "LichSuId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CauHoiNganHangDes_MaNHD",
                table: "CauHoiNganHangDes",
                column: "MaNHD");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietBaiLams_LichSuId",
                table: "ChiTietBaiLams",
                column: "LichSuId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietBaiLams_MaCauHoi",
                table: "ChiTietBaiLams",
                column: "MaCauHoi");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietLopHocs_MaLopHoc",
                table: "ChiTietLopHocs",
                column: "MaLopHoc");

            migrationBuilder.CreateIndex(
                name: "IX_DanhSachDeThiCuaLopHocs_MaDe",
                table: "DanhSachDeThiCuaLopHocs",
                column: "MaDe");

            migrationBuilder.CreateIndex(
                name: "IX_DeThis_MaNHD",
                table: "DeThis",
                column: "MaNHD");

            migrationBuilder.CreateIndex(
                name: "IX_DeThis_UserID",
                table: "DeThis",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuLamBais_MaDe",
                table: "LichSuLamBais",
                column: "MaDe");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuLamBais_MaLopHoc",
                table: "LichSuLamBais",
                column: "MaLopHoc");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuLamBais_UserId",
                table: "LichSuLamBais",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LopHocs_UserId",
                table: "LopHocs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaTranDeThis_MaDe",
                table: "MaTranDeThis",
                column: "MaDe");

            migrationBuilder.CreateIndex(
                name: "IX_NganHangDes_UserId",
                table: "NganHangDes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyHoatDongs_MaDe",
                table: "NhatKyHoatDongs",
                column: "MaDe");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyHoatDongs_MaLopHoc",
                table: "NhatKyHoatDongs",
                column: "MaLopHoc");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyHoatDongs_UserId",
                table: "NhatKyHoatDongs",
                column: "UserId");
        }
    }
}
