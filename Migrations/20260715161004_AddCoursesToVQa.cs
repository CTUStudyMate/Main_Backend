using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCoursesToVQa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerifiableQaCourses",
                columns: table => new
                {
                    CoursesCourseId = table.Column<int>(type: "integer", nullable: false),
                    VerifiableQaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifiableQaCourses", x => new { x.CoursesCourseId, x.VerifiableQaId });
                    table.ForeignKey(
                        name: "FK_VerifiableQaCourses_Courses_CoursesCourseId",
                        column: x => x.CoursesCourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VerifiableQaCourses_VerifiableQas_VerifiableQaId",
                        column: x => x.VerifiableQaId,
                        principalTable: "VerifiableQas",
                        principalColumn: "VerifiableQaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerifiableQaCourses_VerifiableQaId",
                table: "VerifiableQaCourses",
                column: "VerifiableQaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerifiableQaCourses");
        }
    }
}
