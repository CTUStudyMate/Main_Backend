using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadedDocumentStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "Documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingMessage",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingProgress",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessingRunId",
                table: "Documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "ready");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingUpdatedAt",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "Documents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ProcessingMessage",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ProcessingProgress",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ProcessingRunId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ProcessingUpdatedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "Documents");
        }
    }
}
