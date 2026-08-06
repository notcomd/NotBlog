using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markdown.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignMarkDownStatusDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "MarkDown",
                type: "integer",
                nullable: false,
                defaultValue: 15,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 17);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "MarkDown",
                type: "integer",
                nullable: false,
                defaultValue: 17,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 15);
        }
    }
}
