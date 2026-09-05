using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileDev.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropFileChunkRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileChunkRecord");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileChunkRecord",
                columns: table => new
                {
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkSize = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FileDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileIdentity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FileKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FileMd5 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FileTags = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FileType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TotalChunks = table.Column<int>(type: "integer", nullable: false),
                    TotalSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedChunksCsv = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileChunkRecord", x => x.RecordId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileChunkRecord_FileKey",
                table: "FileChunkRecord",
                column: "FileKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileChunkRecord_Status_CreatedAt",
                table: "FileChunkRecord",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FileChunkRecord_UserId",
                table: "FileChunkRecord",
                column: "UserId");
        }
    }
}
