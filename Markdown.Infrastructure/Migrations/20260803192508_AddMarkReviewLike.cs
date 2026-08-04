using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markdown.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkReviewLike : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "OldMarkDown",
                newName: "AuthType");

            migrationBuilder.CreateSequence(
                name: "MarkReviewLikeGuid",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "MarkReviewLike",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MarkReviewGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkReviewLike", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarkReviewLike_MarkReviewGuid",
                table: "MarkReviewLike",
                column: "MarkReviewGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MarkReviewLike_MarkReviewGuid_UserId",
                table: "MarkReviewLike",
                columns: new[] { "MarkReviewGuid", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarkReviewLike");

            migrationBuilder.DropSequence(
                name: "MarkReviewLikeGuid");

            migrationBuilder.RenameColumn(
                name: "AuthType",
                table: "OldMarkDown",
                newName: "Status");
        }
    }
}
