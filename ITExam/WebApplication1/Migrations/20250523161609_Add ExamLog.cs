using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITExam.Migrations
{
    /// <inheritdoc />
    public partial class AddExamLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NhatKyHoatDongs",
                columns: table => new
                {
                    MaNhatKy = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MaDe = table.Column<int>(type: "int", nullable: false),
                    MaLopHoc = table.Column<int>(type: "int", nullable: false),
                    ChuoiGhiNhatKy = table.Column<string>(type: "nvarchar(max)", nullable: false),
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NhatKyHoatDongs");
        }
    }
}
