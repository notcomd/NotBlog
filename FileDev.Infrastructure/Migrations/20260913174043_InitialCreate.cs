using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileDev.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "NotFile",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FileTags = table.Column<List<string>>(type: "text[]", nullable: false),
                    FileDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileUri = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    FileMd5 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileIdentity = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    UploadTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    VolumeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ShardCount = table.Column<int>(type: "integer", nullable: false),
                    StorageExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StorageUpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotFile", x => x.FileId);
                });

            migrationBuilder.CreateTable(
                name: "NotFileTag",
                columns: table => new
                {
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TagDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileIds = table.Column<string>(type: "text", maxLength: -1, nullable: false),
                    DefaultKind = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UploadTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotFileTag", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "NotFileVolume",
                columns: table => new
                {
                    VolumeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RootPath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    TotalBytes = table.Column<long>(type: "bigint", nullable: false),
                    PartCount = table.Column<long>(type: "bigint", nullable: false),
                    ObjectCount = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotFileVolume", x => x.VolumeId);
                });

            migrationBuilder.CreateTable(
                name: "UserFileInfo",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalQuotaBytes = table.Column<long>(type: "bigint", nullable: false),
                    UsedBytes = table.Column<long>(type: "bigint", nullable: false),
                    UpdateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFileInfo", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentAttachmentRef_Content",
                table: "ContentAttachmentRef",
                columns: new[] { "ContentId", "ContentType" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentAttachmentRef_FileUri",
                table: "ContentAttachmentRef",
                column: "FileUri");

            migrationBuilder.CreateIndex(
                name: "IX_NotFile_FileId",
                table: "NotFile",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotFile_FileMd5_FileSize",
                table: "NotFile",
                columns: new[] { "FileMd5", "FileSize" });

            migrationBuilder.CreateIndex(
                name: "IX_NotFile_FileUri",
                table: "NotFile",
                column: "FileUri");

            migrationBuilder.CreateIndex(
                name: "IX_NotFile_UserId",
                table: "NotFile",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotFile_UserId_IsDeleted",
                table: "NotFile",
                columns: new[] { "UserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_NotFile_UserId_Source",
                table: "NotFile",
                columns: new[] { "UserId", "Source" });

            migrationBuilder.CreateIndex(
                name: "IX_NotFile_VolumeId",
                table: "NotFile",
                column: "VolumeId");

            migrationBuilder.CreateIndex(
                name: "IX_NotFileTag_UserId",
                table: "NotFileTag",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotFileTag_UserId_DefaultKind",
                table: "NotFileTag",
                columns: new[] { "UserId", "DefaultKind" });

            migrationBuilder.CreateIndex(
                name: "IX_NotFileTag_UserId_TagName",
                table: "NotFileTag",
                columns: new[] { "UserId", "TagName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotFileVolume_TenantId",
                table: "NotFileVolume",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NotFileVolume_VolumeId",
                table: "NotFileVolume",
                column: "VolumeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientRequest");

            migrationBuilder.DropTable(
                name: "ContentAttachmentRef");

            migrationBuilder.DropTable(
                name: "NotFile");

            migrationBuilder.DropTable(
                name: "NotFileTag");

            migrationBuilder.DropTable(
                name: "NotFileVolume");

            migrationBuilder.DropTable(
                name: "UserFileInfo");
        }
    }
}
