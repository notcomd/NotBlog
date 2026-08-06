using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markdown.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeadMarkHistoryAndMarkOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarkHistory");

            migrationBuilder.DropColumn(
                name: "MarkOption",
                table: "MarkDown");

            migrationBuilder.DropSequence(
                name: "MarkHistoryseq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "MarkHistoryseq",
                incrementBy: 10);

            migrationBuilder.AddColumn<int>(
                name: "MarkOption",
                table: "MarkDown",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MarkHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    MarkDownId = table.Column<int>(type: "integer", nullable: true),
                    LastReadTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MarkDownGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkHistoryGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    ReadCount = table.Column<int>(type: "integer", nullable: false),
                    ReadTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadingProgress = table.Column<int>(type: "integer", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_MarkHistory_MarkDownId",
                table: "MarkHistory",
                column: "MarkDownId");
        }
    }
}
