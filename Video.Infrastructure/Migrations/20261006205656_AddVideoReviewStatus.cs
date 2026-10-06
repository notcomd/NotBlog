using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Video.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoReviewStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "Video",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Video",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Draft");

            // 存量数据回填：状态机引入前「是否公开」由 VideoControl.VideoDisplay 承担，
            // 这里把当前已公开展示（VideoDisplay == true）的存量视频回填为 Approved，
            // 以维持「Status == Approved ⟺ VideoDisplay == true」不变量，避免它们因默认 Draft 而从列表消失。
            migrationBuilder.Sql(
                "UPDATE \"Video\" SET \"Status\" = 'Approved' WHERE \"VideoControl\" ->> 'VideoDisplay' = 'true';");

            migrationBuilder.CreateIndex(
                name: "IX_Video_Status",
                table: "Video",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Video_Status",
                table: "Video");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "Video");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Video");
        }
    }
}
