using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileDev.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BlogFileStrong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "NotFileGroupseq",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "NotFileSeq",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "ClientRequest",
                columns: table => new
                {
                    ClientRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientRequestName = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientRequest", x => x.ClientRequestId);
                });

            migrationBuilder.CreateTable(
                name: "FileChunkRecord",
                columns: table => new
                {
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TotalSize = table.Column<long>(type: "bigint", nullable: false),
                    ChunkSize = table.Column<int>(type: "integer", nullable: false),
                    TotalChunks = table.Column<int>(type: "integer", nullable: false),
                    UploadedChunksCsv = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    FileMd5 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FileIdentity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FileTags = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FileDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileChunkRecord", x => x.RecordId);
                });

            migrationBuilder.CreateTable(
                name: "NotFile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileTags = table.Column<HashSet<string>>(type: "text[]", nullable: false),
                    FileDescription = table.Column<string>(type: "text", nullable: false),
                    FileType = table.Column<int>(type: "integer", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileUri = table.Column<string>(type: "text", nullable: false),
                    FileMd5 = table.Column<string>(type: "text", nullable: false),
                    FileIdentity = table.Column<int>(type: "integer", nullable: false),
                    UploadTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotFile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotFileGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    NotFileGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileGroupName = table.Column<string>(type: "text", nullable: false),
                    FileGroupTags = table.Column<HashSet<string>>(type: "text[]", nullable: false),
                    FileGroupDescription = table.Column<string>(type: "text", nullable: true),
                    UploadTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    FileIdentity = table.Column<int>(type: "integer", nullable: false),
                    FileType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotFileGroup", x => x.Id);
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientRequest");

            migrationBuilder.DropTable(
                name: "FileChunkRecord");

            migrationBuilder.DropTable(
                name: "NotFile");

            migrationBuilder.DropTable(
                name: "NotFileGroup");

            migrationBuilder.DropSequence(
                name: "NotFileGroupseq");

            migrationBuilder.DropSequence(
                name: "NotFileSeq");
        }
    }
}
