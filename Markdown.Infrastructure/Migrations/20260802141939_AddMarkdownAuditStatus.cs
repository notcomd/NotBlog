using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markdown.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkdownAuditStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "MarkDown",
                type: "integer",
                nullable: false,
                defaultValue: 17);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "MarkDown");
        }
    }
}
