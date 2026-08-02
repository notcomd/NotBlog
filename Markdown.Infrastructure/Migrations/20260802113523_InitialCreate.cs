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
                name: "MarkDownGuid",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "MarkHistoryseq",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "MarkReviewGuid",
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
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                    MarkOption = table.Column<int>(type: "integer", nullable: false),
                    MarkDownHash = table.Column<string>(type: "text", nullable: false),
                    MarkDownContent = table.Column<string>(type: "text", nullable: false),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false),
                    CreateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkDown", x => x.Id);
                    table.UniqueConstraint("AK_MarkDown_MarkDownGuid", x => x.MarkDownGuid);
                });

            migrationBuilder.CreateTable(
                name: "MarkHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MarkHistoryGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkDownGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadingProgress = table.Column<int>(type: "integer", nullable: false),
                    ReadTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastReadTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    ReadCount = table.Column<int>(type: "integer", nullable: false),
                    MarkDownId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarkHistory_MarkDown_MarkDownId",
                        column: x => x.MarkDownId,
                        principalTable: "MarkDown",
                        principalColumn: "Id");
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
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                name: "IX_MarkHistory_MarkDownId",
                table: "MarkHistory",
                column: "MarkDownId");

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
                name: "MarkHistory");

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
                name: "MarkHistoryseq");

            migrationBuilder.DropSequence(
                name: "MarkReviewGuid");

            migrationBuilder.DropSequence(
                name: "ReviewImageGuid");
        }
    }
}
