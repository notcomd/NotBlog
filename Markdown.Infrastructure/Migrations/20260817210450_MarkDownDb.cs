using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Markdown.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MarkDownDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "MarkDownGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "MarkFavoriteGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "MarkFavoriteTagGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "MarkReviewGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "MarkReviewLikeGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "ReviewImageGuid",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "ClientRequest",
                columns: table => new
                {
                    ClientRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientRequestName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResponseJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientRequest", x => x.ClientRequestId);
                });

            migrationBuilder.CreateTable(
                name: "MarkDown",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MarkDownGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkReviewGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkUserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkDownName = table.Column<string>(type: "text", nullable: false),
                    MarkDownTagboard = table.Column<string>(type: "text", nullable: false),
                    MarkDownAuth = table.Column<int>(type: "integer", nullable: false),
                    MarkDownHash = table.Column<string>(type: "text", nullable: false),
                    MarkDownContent = table.Column<string>(type: "character varying(1000000)", maxLength: 1000000, nullable: false),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    CreateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkDown", x => x.Id);
                    table.UniqueConstraint("AK_MarkDown_MarkDownGuid", x => x.MarkDownGuid);
                });

            migrationBuilder.CreateTable(
                name: "MarkFavorite",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MarkFavoriteGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkDownGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    CreateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TagsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkFavorite", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarkFavoriteTag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MarkFavoriteTagGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Tag = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UseCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkFavoriteTag", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "MarkReview",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MarkReviewGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkDownGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkAggregateRootGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    MarkReviewContent = table.Column<string>(type: "text", nullable: true),
                    MarkReviewTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MarkReviewAuth = table.Column<int>(type: "integer", nullable: false),
                    LoveCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    ReplyCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CommentCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    ShareCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    ViewCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkReview", x => x.Id);
                    table.UniqueConstraint("AK_MarkReview_MarkReviewGuid", x => x.MarkReviewGuid);
                    table.ForeignKey(
                        name: "FK_MarkReview_MarkDown_MarkDownGuid",
                        column: x => x.MarkDownGuid,
                        principalTable: "MarkDown",
                        principalColumn: "MarkDownGuid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarkReview_MarkReview_MarkAggregateRootGuid",
                        column: x => x.MarkAggregateRootGuid,
                        principalTable: "MarkReview",
                        principalColumn: "MarkReviewGuid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OldMarkDown",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OldMarkDownGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkDownGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthType = table.Column<int>(type: "integer", nullable: false),
                    OldMarkDownContent = table.Column<string>(type: "text", nullable: false),
                    OldMarkDownHash = table.Column<string>(type: "text", nullable: false),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false),
                    CreateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OldMarkDown", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OldMarkDown_MarkDown_MarkDownGuid",
                        column: x => x.MarkDownGuid,
                        principalTable: "MarkDown",
                        principalColumn: "MarkDownGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    ImageName = table.Column<string>(type: "text", nullable: false),
                    MarkReviewId = table.Column<int>(type: "integer", nullable: false)
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
                name: "IX_MarkFavorite_MarkDownGuid",
                table: "MarkFavorite",
                column: "MarkDownGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MarkFavorite_UserGuid",
                table: "MarkFavorite",
                column: "UserGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MarkFavorite_UserGuid_MarkDownGuid",
                table: "MarkFavorite",
                columns: new[] { "UserGuid", "MarkDownGuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarkFavoriteTag_UserGuid",
                table: "MarkFavoriteTag",
                column: "UserGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MarkFavoriteTag_UserGuid_Tag",
                table: "MarkFavoriteTag",
                columns: new[] { "UserGuid", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarkReview_MarkAggregateRootGuid",
                table: "MarkReview",
                column: "MarkAggregateRootGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MarkReview_MarkDownGuid",
                table: "MarkReview",
                column: "MarkDownGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MarkReview_UserId",
                table: "MarkReview",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MarkReviewLike_MarkReviewGuid",
                table: "MarkReviewLike",
                column: "MarkReviewGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MarkReviewLike_MarkReviewGuid_UserId",
                table: "MarkReviewLike",
                columns: new[] { "MarkReviewGuid", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OldMarkDown_MarkDownGuid",
                table: "OldMarkDown",
                column: "MarkDownGuid");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewImage_MarkReviewId",
                table: "ReviewImage",
                column: "MarkReviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientRequest");

            migrationBuilder.DropTable(
                name: "MarkFavorite");

            migrationBuilder.DropTable(
                name: "MarkFavoriteTag");

            migrationBuilder.DropTable(
                name: "MarkReviewLike");

            migrationBuilder.DropTable(
                name: "OldMarkDown");

            migrationBuilder.DropTable(
                name: "ReviewImage");

            migrationBuilder.DropTable(
                name: "MarkReview");

            migrationBuilder.DropTable(
                name: "MarkDown");

            migrationBuilder.DropSequence(
                name: "MarkDownGuid");

            migrationBuilder.DropSequence(
                name: "MarkFavoriteGuid");

            migrationBuilder.DropSequence(
                name: "MarkFavoriteTagGuid");

            migrationBuilder.DropSequence(
                name: "MarkReviewGuid");

            migrationBuilder.DropSequence(
                name: "MarkReviewLikeGuid");

            migrationBuilder.DropSequence(
                name: "ReviewImageGuid");
        }
    }
}
