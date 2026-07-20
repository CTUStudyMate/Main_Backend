using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddBackgroundJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackgroundJobs",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    BusinessData = table.Column<string>(type: "jsonb", nullable: true),
                    SourceEntityType = table.Column<string>(type: "text", nullable: false),
                    VerifiableQaId = table.Column<int>(type: "integer", nullable: true),
                    CuratedQaId = table.Column<int>(type: "integer", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobs", x => x.JobId);
                    table.CheckConstraint("CK_BackgroundJobs_ValidSourceEntity", "(\"SourceEntityType\" = 'verifiable_qa'\n    AND \"VerifiableQaId\" IS NOT NULL\n    AND \"CuratedQaId\" IS NULL)\nOR\n(\"SourceEntityType\" = 'curated_qa'\n    AND \"CuratedQaId\" IS NOT NULL\n    AND \"VerifiableQaId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_BackgroundJobs_CuratedQas_CuratedQaId",
                        column: x => x.CuratedQaId,
                        principalTable: "CuratedQas",
                        principalColumn: "CuratedQaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BackgroundJobs_VerifiableQas_VerifiableQaId",
                        column: x => x.VerifiableQaId,
                        principalTable: "VerifiableQas",
                        principalColumn: "VerifiableQaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundJobLogs",
                columns: table => new
                {
                    BackgroundJobLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobLogs", x => x.BackgroundJobLogId);
                    table.ForeignKey(
                        name: "FK_BackgroundJobLogs_BackgroundJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "BackgroundJobs",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobLogs_JobId",
                table: "BackgroundJobLogs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_CuratedQaId",
                table: "BackgroundJobs",
                column: "CuratedQaId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobs_VerifiableQaId",
                table: "BackgroundJobs",
                column: "VerifiableQaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundJobLogs");

            migrationBuilder.DropTable(
                name: "BackgroundJobs");
        }
    }
}
