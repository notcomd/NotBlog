using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markdown.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReviewImagesAsOwnedJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewImage");

            migrationBuilder.DropSequence(
                name: "MarkCoinGuid");

            migrationBuilder.DropSequence(
                name: "MarkFavoriteTagGuid");

            migrationBuilder.DropSequence(
                name: "MarkReviewGuid");

            migrationBuilder.DropSequence(
                name: "ReviewImageGuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "MarkReview",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "ReviewImages",
                table: "MarkReview",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "MarkFavoriteTag",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "MarkCoin",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "MarkCoinGuid",
                table: "MarkCoin",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewImages",
                table: "MarkReview");

            migrationBuilder.DropColumn(
                name: "MarkCoinGuid",
                table: "MarkCoin");

            migrationBuilder.CreateSequence(
                name: "MarkCoinGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "MarkFavoriteTagGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "MarkReviewGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "ReviewImageGuid",
                incrementBy: 10);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "MarkReview",
                type: "integer",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "MarkFavoriteTag",
                type: "integer",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "MarkCoin",
                type: "integer",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "ReviewImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MarkReviewId = table.Column<int>(type: "integer", nullable: false),
                    ImageName = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewImage_MarkReview_MarkReviewId",
                        column: x => x.MarkReviewId,
                        principalTable: "MarkReview",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewImage_MarkReviewId",
                table: "ReviewImage",
                column: "MarkReviewId");
        }
    }
}
