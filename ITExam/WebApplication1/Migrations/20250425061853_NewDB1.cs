using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITExam.Migrations
{
    /// <inheritdoc />
    public partial class NewDB1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Lop = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    VaiTro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Khoa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "LopHocs",
                columns: table => new
                {
                    MaLopHoc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLopHoc = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
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
                    TenNHD = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    MaMonHoc = table.Column<int>(type: "int", nullable: false),
                    LoaiDe = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
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
                    NoiDungCauHoi = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NoiDungLuaChon = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MaCLO = table.Column<int>(type: "int", nullable: true),
                    MaChuong = table.Column<int>(type: "int", nullable: true),
                    LoaiCauHoi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaNHD = table.Column<int>(type: "int", nullable: false),
                    DiemCauHoi = table.Column<double>(type: "float", nullable: true)
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
                    TenDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiGianLamBai = table.Column<int>(type: "int", nullable: false),
                    LoaiDe = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaNHD = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false)
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
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ThoiGianBatDauLamBai = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiGianNop = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThoiGianLamBai = table.Column<int>(type: "int", nullable: false),
                    Diem = table.Column<double>(type: "float", nullable: false),
                    MaDe = table.Column<int>(type: "int", nullable: false),
                    MaLopHoc = table.Column<int>(type: "int", nullable: false)
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
                    MaChuong = table.Column<int>(type: "int", nullable: false),
                    MaCLO = table.Column<int>(type: "int", nullable: false),
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
                name: "ChiTietBaiLams",
                columns: table => new
                {
                    ChiTietBaiLamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaCauHoi = table.Column<int>(type: "int", nullable: false),
                    LichSuId = table.Column<int>(type: "int", nullable: false),
                    CauTraLoiTuLuan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CauTraLoiTN = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "CauHoiNganHangDes");

            migrationBuilder.DropTable(
                name: "LichSuLamBais");

            migrationBuilder.DropTable(
                name: "DeThis");

            migrationBuilder.DropTable(
                name: "LopHocs");

            migrationBuilder.DropTable(
                name: "NganHangDes");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
