using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markdown.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MarkdownFileStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentCount",
                table: "MarkReview");

            migrationBuilder.DropColumn(
                name: "MarkDownContent",
                table: "MarkDown");

            migrationBuilder.DropColumn(
                name: "ShareCount",
                table: "MarkReview");

            migrationBuilder.AddColumn<long>(
                name: "DislikeCount",
                table: "MarkReview",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateSequence(
                name: "MarkCoinGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "MarkDocumentLikeGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "MarkReviewDislikeGuid",
                incrementBy: 10);

            migrationBuilder.AlterColumn<string>(
                name: "MarkDownHash",
                table: "MarkDown",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<long>(
                name: "CoinCount",
                table: "MarkDown",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "FavoriteCount",
                table: "MarkDown",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "FileExt",
                table: "MarkDown",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileId",
                table: "MarkDown",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "MarkDown",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "FileUri",
                table: "MarkDown",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "HeatScore",
                table: "MarkDown",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<long>(
                name: "LoveCount",
                table: "MarkDown",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ShareCount",
                table: "MarkDown",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ViewCount",
                table: "MarkDown",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "MarkCoin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MarkDownGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkCoin", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarkDocumentLike",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MarkDownGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkDocumentLike", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarkReviewDislike",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MarkReviewGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkReviewDislike", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarkCoin_MarkDownGuid",
                table: "MarkCoin",
                column: "MarkDownGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MarkDocumentLike_MarkDownGuid",
                table: "MarkDocumentLike",
                column: "MarkDownGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MarkDocumentLike_MarkDownGuid_UserId",
                table: "MarkDocumentLike",
                columns: new[] { "MarkDownGuid", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarkReviewDislike_MarkReviewGuid",
                table: "MarkReviewDislike",
                column: "MarkReviewGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MarkReviewDislike_MarkReviewGuid_UserId",
                table: "MarkReviewDislike",
                columns: new[] { "MarkReviewGuid", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarkCoin");

            migrationBuilder.DropTable(
                name: "MarkDocumentLike");

            migrationBuilder.DropTable(
                name: "MarkReviewDislike");

            migrationBuilder.DropColumn(
                name: "CoinCount",
                table: "MarkDown");

            migrationBuilder.DropColumn(
                name: "FavoriteCount",
                table: "MarkDown");

            migrationBuilder.DropColumn(
                name: "FileExt",
                table: "MarkDown");

            migrationBuilder.DropColumn(
                name: "FileId",
                table: "MarkDown");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "MarkDown");

            migrationBuilder.DropColumn(
                name: "FileUri",
                table: "MarkDown");

            migrationBuilder.DropColumn(
                name: "HeatScore",
                table: "MarkDown");

            migrationBuilder.DropColumn(
                name: "LoveCount",
                table: "MarkDown");

            migrationBuilder.DropColumn(
                name: "ShareCount",
                table: "MarkDown");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "MarkDown");

            migrationBuilder.DropSequence(
                name: "MarkCoinGuid");

            migrationBuilder.DropSequence(
                name: "MarkDocumentLikeGuid");

            migrationBuilder.DropSequence(
                name: "MarkReviewDislikeGuid");

            migrationBuilder.DropColumn(
                name: "DislikeCount",
                table: "MarkReview");

            migrationBuilder.AddColumn<long>(
                name: "ShareCount",
                table: "MarkReview",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CommentCount",
                table: "MarkReview",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<string>(
                name: "MarkDownHash",
                table: "MarkDown",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "MarkDownContent",
                table: "MarkDown",
                type: "character varying(1000000)",
                maxLength: 1000000,
                nullable: false,
                defaultValue: "");
        }
    }
}
