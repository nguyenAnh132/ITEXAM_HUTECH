using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITExam.Migrations
{
    /// <inheritdoc />
    public partial class updateGoogleDriveId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UploadedFileId",
                table: "ExamHistory",
                type: "varchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadedFileId",
                table: "ExamHistory");
        }
    }
}
