using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Video.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Video",
                columns: table => new
                {
                    VideoGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Affiliated = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    VideoCover = table.Column<string>(type: "text", nullable: false),
                    VideoName = table.Column<string>(type: "text", nullable: false),
                    BriefIntroduction = table.Column<string>(type: "text", nullable: false),
                    VideoTags = table.Column<List<string>>(type: "text[]", nullable: false),
                    VideoFileUri = table.Column<string>(type: "text", nullable: false),
                    VideoNvid = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TimeSpace = table.Column<string>(type: "jsonb", nullable: false),
                    VideoControl = table.Column<string>(type: "jsonb", nullable: false),
                    VideoQuote = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Video", x => x.VideoGuid);
                });

            migrationBuilder.CreateTable(
                name: "VideoCollection",
                columns: table => new
                {
                    VideoCollectionGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoNvid = table.Column<string>(type: "text", nullable: false),
                    VideoCollectionName = table.Column<string>(type: "text", nullable: false),
                    VideoCollectionBriefIntroduction = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeSpace = table.Column<string>(type: "jsonb", nullable: false),
                    VideoControl = table.Column<string>(type: "jsonb", nullable: false),
                    VideoQuote = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoCollection", x => x.VideoCollectionGuid);
                });

            migrationBuilder.CreateTable(
                name: "VideoHistory",
                columns: table => new
                {
                    VideoHistoryGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Progress = table.Column<double>(type: "double precision", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastPositionSeconds = table.Column<double>(type: "double precision", nullable: false),
                    TimeSpace = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoHistory", x => x.VideoHistoryGuid);
                });

            migrationBuilder.CreateTable(
                name: "VideoBarrage",
                columns: table => new
                {
                    VideoBarrageGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    BarrageType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "Text"),
                    VideoBarrageBody = table.Column<string>(type: "text", nullable: true),
                    IsDelete = table.Column<bool>(type: "boolean", nullable: false),
                    TimeAt = table.Column<long>(type: "bigint", nullable: true),
                    VideosVideoGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    TimeSpace = table.Column<string>(type: "jsonb", nullable: false),
                    VideoControl = table.Column<string>(type: "jsonb", nullable: false),
                    VideoImages = table.Column<string>(type: "jsonb", nullable: true),
                    VideoQuote = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoBarrage", x => x.VideoBarrageGuid);
                    table.ForeignKey(
                        name: "FK_VideoBarrage_Video_VideosVideoGuid",
                        column: x => x.VideosVideoGuid,
                        principalTable: "Video",
                        principalColumn: "VideoGuid");
                });

            migrationBuilder.CreateTable(
                name: "VideoReview",
                columns: table => new
                {
                    VideoReviewGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    RootReview = table.Column<Guid>(type: "uuid", nullable: true),
                    Content = table.Column<string>(type: "jsonb", nullable: false),
                    Quote = table.Column<string>(type: "jsonb", nullable: false),
                    TimeSpace = table.Column<string>(type: "jsonb", nullable: false),
                    VideoControl = table.Column<string>(type: "jsonb", nullable: false),
                    VideoImages = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoReview", x => x.VideoReviewGuid);
                    table.ForeignKey(
                        name: "FK_VideoReview_VideoReview_VideoGuid",
                        column: x => x.VideoGuid,
                        principalTable: "VideoReview",
                        principalColumn: "VideoReviewGuid");
                    table.ForeignKey(
                        name: "FK_VideoReview_Video_VideoGuid",
                        column: x => x.VideoGuid,
                        principalTable: "Video",
                        principalColumn: "VideoGuid");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Video_VideoName",
                table: "Video",
                column: "VideoName");

            migrationBuilder.CreateIndex(
                name: "IX_Video_VideoNvid",
                table: "Video",
                column: "VideoNvid");

            migrationBuilder.CreateIndex(
                name: "IX_Video_VideoTags",
                table: "Video",
                column: "VideoTags");

            migrationBuilder.CreateIndex(
                name: "IX_VideoBarrage_UserGuid",
                table: "VideoBarrage",
                column: "UserGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VideoBarrage_VideoGuid",
                table: "VideoBarrage",
                column: "VideoGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VideoBarrage_VideosVideoGuid",
                table: "VideoBarrage",
                column: "VideosVideoGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VideoCollection_VideoCollectionGuid",
                table: "VideoCollection",
                column: "VideoCollectionGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VideoHistory_VideoHistoryGuid",
                table: "VideoHistory",
                column: "VideoHistoryGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VideoReview_RootReview",
                table: "VideoReview",
                column: "RootReview");

            migrationBuilder.CreateIndex(
                name: "IX_VideoReview_UserGuid",
                table: "VideoReview",
                column: "UserGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VideoReview_VideoGuid",
                table: "VideoReview",
                column: "VideoGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VideoReview_VideoReviewGuid",
                table: "VideoReview",
                column: "VideoReviewGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoBarrage");

            migrationBuilder.DropTable(
                name: "VideoCollection");

            migrationBuilder.DropTable(
                name: "VideoHistory");

            migrationBuilder.DropTable(
                name: "VideoReview");

            migrationBuilder.DropTable(
                name: "Video");
        }
    }
}
