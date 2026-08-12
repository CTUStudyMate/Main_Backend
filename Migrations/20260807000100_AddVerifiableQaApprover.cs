using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainBackend.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260807000100_AddVerifiableQaApprover")]
    public partial class AddVerifiableQaApprover : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "VerifiableQas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerifiableQas_ApprovedByUserId",
                table: "VerifiableQas",
                column: "ApprovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_VerifiableQas_Users_ApprovedByUserId",
                table: "VerifiableQas",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VerifiableQas_Users_ApprovedByUserId",
                table: "VerifiableQas");

            migrationBuilder.DropIndex(
                name: "IX_VerifiableQas_ApprovedByUserId",
                table: "VerifiableQas");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "VerifiableQas");
        }
    }
}
