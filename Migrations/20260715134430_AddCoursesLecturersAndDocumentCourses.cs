using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCoursesLecturersAndDocumentCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CourseCode",
                table: "Courses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DocumentCourses",
                columns: table => new
                {
                    CoursesCourseId = table.Column<int>(type: "integer", nullable: false),
                    DocumentId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentCourses", x => new { x.CoursesCourseId, x.DocumentId });
                    table.ForeignKey(
                        name: "FK_DocumentCourses_Courses_CoursesCourseId",
                        column: x => x.CoursesCourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentCourses_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LecturerCourses",
                columns: table => new
                {
                    CoursesCourseId = table.Column<int>(type: "integer", nullable: false),
                    LecturersUserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LecturerCourses", x => new { x.CoursesCourseId, x.LecturersUserId });
                    table.ForeignKey(
                        name: "FK_LecturerCourses_Courses_CoursesCourseId",
                        column: x => x.CoursesCourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LecturerCourses_Users_LecturersUserId",
                        column: x => x.LecturersUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentCourses_DocumentId",
                table: "DocumentCourses",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_LecturerCourses_LecturersUserId",
                table: "LecturerCourses",
                column: "LecturersUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentCourses");

            migrationBuilder.DropTable(
                name: "LecturerCourses");

            migrationBuilder.DropColumn(
                name: "CourseCode",
                table: "Courses");
        }
    }
}
