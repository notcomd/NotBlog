using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markdown.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignCurrentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MarkDownContent",
                table: "MarkDown",
                type: "character varying(1000000)",
                maxLength: 1000000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MarkDownContent",
                table: "MarkDown",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000000)",
                oldMaxLength: 1000000);
        }
    }
}
