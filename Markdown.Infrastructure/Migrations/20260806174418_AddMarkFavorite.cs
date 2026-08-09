using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markdown.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkFavorite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "MarkFavoriteGuid",
                incrementBy: 10);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarkFavorite");

            migrationBuilder.DropSequence(
                name: "MarkFavoriteGuid");
        }
    }
}
