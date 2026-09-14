using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Markdown.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "MarkDocumentLikeGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "MarkDownGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "MarkFavoriteGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "MarkReviewDislikeGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "MarkReviewLikeGuid",
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
                name: "MarkCoin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkCoinGuid = table.Column<Guid>(type: "uuid", nullable: false),
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
                    MarkDownHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FileUri = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    FileExt = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CoverUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    CreateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LoveCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    FavoriteCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    ShareCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CoinCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    ViewCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    HeatScore = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0)
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
                    MarkFavoriteTagGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Tag = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UseCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkFavoriteTag", x => x.MarkFavoriteTagGuid);
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
                    MarkDownGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkReviewGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkAggregateRootGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    MarkReviewContent = table.Column<string>(type: "text", nullable: true),
                    MarkReviewTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MarkReviewAuth = table.Column<int>(type: "integer", nullable: false),
                    LoveCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    ViewCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    ReplyCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    DislikeCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewImages = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkReview", x => x.MarkDownGuid);
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

            migrationBuilder.CreateIndex(
                name: "IX_MarkCoin_MarkDownGuid",
                table: "MarkCoin",
                column: "MarkDownGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MarkCoin_MarkDownGuid_UserId",
                table: "MarkCoin",
                columns: new[] { "MarkDownGuid", "UserId" },
                unique: true);

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
                name: "IX_MarkReviewDislike_MarkReviewGuid",
                table: "MarkReviewDislike",
                column: "MarkReviewGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MarkReviewDislike_MarkReviewGuid_UserId",
                table: "MarkReviewDislike",
                columns: new[] { "MarkReviewGuid", "UserId" },
                unique: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientRequest");

            migrationBuilder.DropTable(
                name: "MarkCoin");

            migrationBuilder.DropTable(
                name: "MarkDocumentLike");

            migrationBuilder.DropTable(
                name: "MarkFavorite");

            migrationBuilder.DropTable(
                name: "MarkFavoriteTag");

            migrationBuilder.DropTable(
                name: "MarkReview");

            migrationBuilder.DropTable(
                name: "MarkReviewDislike");

            migrationBuilder.DropTable(
                name: "MarkReviewLike");

            migrationBuilder.DropTable(
                name: "OldMarkDown");

            migrationBuilder.DropTable(
                name: "MarkDown");

            migrationBuilder.DropSequence(
                name: "MarkDocumentLikeGuid");

            migrationBuilder.DropSequence(
                name: "MarkDownGuid");

            migrationBuilder.DropSequence(
                name: "MarkFavoriteGuid");

            migrationBuilder.DropSequence(
                name: "MarkReviewDislikeGuid");

            migrationBuilder.DropSequence(
                name: "MarkReviewLikeGuid");
        }
    }
}
