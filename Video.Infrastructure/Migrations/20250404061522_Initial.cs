using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Video.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoBarrage",
                columns: table => new
                {
                    VideoBarrageGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoBarrageBody = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoBarrage", x => x.VideoBarrageGuid);
                });

            migrationBuilder.CreateTable(
                name: "VideoCollection",
                columns: table => new
                {
                    VideoCollectionGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    AffiliatedUser = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    VideoCollectionName = table.Column<string>(type: "text", nullable: false),
                    VideoCollectionBriefIntroduction = table.Column<string>(type: "text", nullable: false),
                    VideoGuid = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    TimeSpace = table.Column<string>(type: "jsonb", nullable: false),
                    VideoControl = table.Column<string>(type: "jsonb", nullable: false),
                    VideoQuote = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoCollection", x => x.VideoCollectionGuid);
                });

            migrationBuilder.CreateTable(
                name: "Videos",
                columns: table => new
                {
                    VideoGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    AffiliatedUserGuid = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    VideoCover = table.Column<string>(type: "text", nullable: false),
                    VideoName = table.Column<string>(type: "text", nullable: false),
                    BriefIntroduction = table.Column<string>(type: "text", nullable: false),
                    VideoTags = table.Column<List<string>>(type: "text[]", nullable: false),
                    VideoFileUri = table.Column<string>(type: "text", nullable: false),
                    ChildVideosList = table.Column<List<Guid>>(type: "uuid[]", nullable: true),
                    TimeSpace = table.Column<string>(type: "jsonb", nullable: false),
                    VideoControl = table.Column<string>(type: "jsonb", nullable: false),
                    VideoQuote = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.VideoGuid);
                });

            migrationBuilder.CreateTable(
                name: "VideoReview",
                columns: table => new
                {
                    VideoReviewGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    RootReview = table.Column<Guid>(type: "uuid", nullable: true),
                    VideoReviewBody = table.Column<string>(type: "text", nullable: true),
                    TimeSpace = table.Column<string>(type: "jsonb", nullable: false),
                    VideoControl = table.Column<string>(type: "jsonb", nullable: false),
                    VideoQuote = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoReview", x => x.VideoReviewGuid);
                    table.ForeignKey(
                        name: "FK_VideoReview_Videos_VideoGuid",
                        column: x => x.VideoGuid,
                        principalTable: "Videos",
                        principalColumn: "VideoGuid");
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoCollection_VideoCollectionGuid",
                table: "VideoCollection",
                column: "VideoCollectionGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VideoReview_VideoGuid",
                table: "VideoReview",
                column: "VideoGuid");

            migrationBuilder.CreateIndex(
                name: "IX_VideoReview_VideoReviewGuid",
                table: "VideoReview",
                column: "VideoReviewGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_VideoGuid",
                table: "Videos",
                column: "VideoGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoBarrage");

            migrationBuilder.DropTable(
                name: "VideoCollection");

            migrationBuilder.DropTable(
                name: "VideoReview");

            migrationBuilder.DropTable(
                name: "Videos");
        }
    }
}
