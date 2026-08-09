using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markdown.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkFavoriteTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "MarkFavoriteTagGuid",
                incrementBy: 10);

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

            migrationBuilder.CreateIndex(
                name: "IX_MarkFavoriteTag_UserGuid",
                table: "MarkFavoriteTag",
                column: "UserGuid");

            migrationBuilder.CreateIndex(
                name: "IX_MarkFavoriteTag_UserGuid_Tag",
                table: "MarkFavoriteTag",
                columns: new[] { "UserGuid", "Tag" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarkFavoriteTag");

            migrationBuilder.DropSequence(
                name: "MarkFavoriteTagGuid");
        }
    }
}
