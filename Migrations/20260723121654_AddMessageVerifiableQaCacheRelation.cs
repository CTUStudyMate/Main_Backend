using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageVerifiableQaCacheRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VerifiableQas_Messages_MessageId",
                table: "VerifiableQas");

            migrationBuilder.RenameColumn(
                name: "MessageId",
                table: "VerifiableQas",
                newName: "SourceMessageId");

            migrationBuilder.RenameIndex(
                name: "IX_VerifiableQas_MessageId",
                table: "VerifiableQas",
                newName: "IX_VerifiableQas_SourceMessageId");

            migrationBuilder.AddColumn<int>(
                name: "VerifiableQaId",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_VerifiableQaId",
                table: "Messages",
                column: "VerifiableQaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_VerifiableQas_VerifiableQaId",
                table: "Messages",
                column: "VerifiableQaId",
                principalTable: "VerifiableQas",
                principalColumn: "VerifiableQaId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VerifiableQas_Messages_SourceMessageId",
                table: "VerifiableQas",
                column: "SourceMessageId",
                principalTable: "Messages",
                principalColumn: "MessageId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_VerifiableQas_VerifiableQaId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_VerifiableQas_Messages_SourceMessageId",
                table: "VerifiableQas");

            migrationBuilder.DropIndex(
                name: "IX_Messages_VerifiableQaId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "VerifiableQaId",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "SourceMessageId",
                table: "VerifiableQas",
                newName: "MessageId");

            migrationBuilder.RenameIndex(
                name: "IX_VerifiableQas_SourceMessageId",
                table: "VerifiableQas",
                newName: "IX_VerifiableQas_MessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_VerifiableQas_Messages_MessageId",
                table: "VerifiableQas",
                column: "MessageId",
                principalTable: "Messages",
                principalColumn: "MessageId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
