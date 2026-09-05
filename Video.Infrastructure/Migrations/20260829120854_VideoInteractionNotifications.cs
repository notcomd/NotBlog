using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Video.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VideoInteractionNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VideoQuote",
                table: "VideoReview",
                newName: "Quote");

            // 存量数据回填：旧键 c_Upvote / c_Down → 新键 c_Like / c_Dislike（缺失键按 0 处理）。
            // 旧 JSON 中其余键（c_Stars/c_Watch/c_Ballot/c_Share）在评论场景中语义废弃，不再保留。
            migrationBuilder.Sql("""
                UPDATE "VideoReview"
                SET "Quote" = jsonb_build_object(
                    'c_Like',    COALESCE(NULLIF("Quote" ->> 'c_Upvote', '')::bigint, 0),
                    'c_Dislike', COALESCE(NULLIF("Quote" ->> 'c_Down', '')::bigint, 0)
                )
                WHERE "Quote" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Quote",
                table: "VideoReview",
                newName: "VideoQuote");
        }
    }
}
