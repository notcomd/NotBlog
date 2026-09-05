using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileDev.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentSourceAndAttachmentRef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "NotFile",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ContentAttachmentRef",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContentType = table.Column<int>(type: "integer", nullable: false),
                    FileUri = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SourceFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActiveRefs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentAttachmentRef", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotFile_UserId_Source",
                table: "NotFile",
                columns: new[] { "UserId", "Source" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentAttachmentRef_Content",
                table: "ContentAttachmentRef",
                columns: new[] { "ContentId", "ContentType" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentAttachmentRef_FileUri",
                table: "ContentAttachmentRef",
                column: "FileUri");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentAttachmentRef");

            migrationBuilder.DropIndex(
                name: "IX_NotFile_UserId_Source",
                table: "NotFile");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "NotFile");
        }
    }
}
