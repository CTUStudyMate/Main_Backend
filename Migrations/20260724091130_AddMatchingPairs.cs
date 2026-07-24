using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace MainBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchingPairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchingPairs",
                columns: table => new
                {
                    MatchingPairId = table.Column<Guid>(type: "uuid", nullable: false),
                    CuratedQaId = table.Column<int>(type: "integer", nullable: false),
                    LeftText = table.Column<string>(type: "text", nullable: false),
                    RightText = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1536)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchingPairs", x => x.MatchingPairId);
                    table.ForeignKey(
                        name: "FK_MatchingPairs_CuratedQas_CuratedQaId",
                        column: x => x.CuratedQaId,
                        principalTable: "CuratedQas",
                        principalColumn: "CuratedQaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchingPairs_CuratedQaId",
                table: "MatchingPairs",
                column: "CuratedQaId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchingPairs_Embedding",
                table: "MatchingPairs",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" })
                .Annotation("Npgsql:StorageParameter:ef_construction", 64)
                .Annotation("Npgsql:StorageParameter:m", 16);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchingPairs");
        }
    }
}
