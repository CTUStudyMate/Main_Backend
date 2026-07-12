using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace MainBackend.Migrations
{
    /// <inheritdoc />
    public partial class RebuildDataModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_UserId",
                table: "Messages");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<string>(
                name: "Cohort",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MajorId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Messages",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.CourseId);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    DocumentTitle = table.Column<string>(type: "text", nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    Visibility = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_Documents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Majors",
                columns: table => new
                {
                    MajorId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MajorCode = table.Column<string>(type: "text", nullable: false),
                    MajorName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Majors", x => x.MajorId);
                });

            migrationBuilder.CreateTable(
                name: "VerifiableQas",
                columns: table => new
                {
                    VerifiableQaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    OriginalQuestion = table.Column<string>(type: "text", nullable: false),
                    RewrittenQuestion = table.Column<string>(type: "text", nullable: true),
                    GeneratedAnswer = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ApprovedAnswer = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1536)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifiableQas", x => x.VerifiableQaId);
                    table.ForeignKey(
                        name: "FK_VerifiableQas_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "MessageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VerifiableQas_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ReviewSessions",
                columns: table => new
                {
                    ReviewSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: true),
                    TotalQuestions = table.Column<int>(type: "integer", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewSessions", x => x.ReviewSessionId);
                    table.ForeignKey(
                        name: "FK_ReviewSessions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId");
                    table.ForeignKey(
                        name: "FK_ReviewSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CuratedQas",
                columns: table => new
                {
                    CuratedQaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VerifiableQaId = table.Column<int>(type: "integer", nullable: false),
                    CuratedQuestion = table.Column<string>(type: "text", nullable: false),
                    CuratedAnswer = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuratedQas", x => x.CuratedQaId);
                    table.ForeignKey(
                        name: "FK_CuratedQas_VerifiableQas_VerifiableQaId",
                        column: x => x.VerifiableQaId,
                        principalTable: "VerifiableQas",
                        principalColumn: "VerifiableQaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionItems",
                columns: table => new
                {
                    QuestionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CuratedQaId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    QuestionData = table.Column<string>(type: "jsonb", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionItems", x => x.QuestionItemId);
                    table.ForeignKey(
                        name: "FK_QuestionItems_CuratedQas_CuratedQaId",
                        column: x => x.CuratedQaId,
                        principalTable: "CuratedQas",
                        principalColumn: "CuratedQaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionResponses",
                columns: table => new
                {
                    QuestionResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentAnswer = table.Column<string>(type: "jsonb", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionResponses", x => x.QuestionResponseId);
                    table.ForeignKey(
                        name: "FK_QuestionResponses_QuestionItems_QuestionItemId",
                        column: x => x.QuestionItemId,
                        principalTable: "QuestionItems",
                        principalColumn: "QuestionItemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionResponses_ReviewSessions_ReviewSessionId",
                        column: x => x.ReviewSessionId,
                        principalTable: "ReviewSessions",
                        principalColumn: "ReviewSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_MajorId",
                table: "Users",
                column: "MajorId");

            migrationBuilder.CreateIndex(
                name: "IX_CuratedQas_VerifiableQaId",
                table: "CuratedQas",
                column: "VerifiableQaId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UserId",
                table: "Documents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionItems_CuratedQaId",
                table: "QuestionItems",
                column: "CuratedQaId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionResponses_QuestionItemId",
                table: "QuestionResponses",
                column: "QuestionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionResponses_ReviewSessionId",
                table: "QuestionResponses",
                column: "ReviewSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewSessions_CourseId",
                table: "ReviewSessions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewSessions_UserId",
                table: "ReviewSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VerifiableQas_MessageId",
                table: "VerifiableQas",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerifiableQas_UserId",
                table: "VerifiableQas",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_UserId",
                table: "Messages",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Majors_MajorId",
                table: "Users",
                column: "MajorId",
                principalTable: "Majors",
                principalColumn: "MajorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_UserId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Majors_MajorId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Majors");

            migrationBuilder.DropTable(
                name: "QuestionResponses");

            migrationBuilder.DropTable(
                name: "QuestionItems");

            migrationBuilder.DropTable(
                name: "ReviewSessions");

            migrationBuilder.DropTable(
                name: "CuratedQas");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "VerifiableQas");

            migrationBuilder.DropIndex(
                name: "IX_Users_MajorId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Cohort",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MajorId",
                table: "Users");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_UserId",
                table: "Messages",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
