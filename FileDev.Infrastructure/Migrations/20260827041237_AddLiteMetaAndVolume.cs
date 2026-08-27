using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileDev.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLiteMetaAndVolume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "NotFile",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ShardCount",
                table: "NotFile",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StorageExpiresAt",
                table: "NotFile",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StorageUpdatedUtc",
                table: "NotFile",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "NotFile",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VolumeId",
                table: "NotFile",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.CreateIndex(
                name: "IX_NotFile_VolumeId",
                table: "NotFile",
                column: "VolumeId");

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
                name: "NotFileVolume");

            migrationBuilder.DropIndex(
                name: "IX_NotFile_VolumeId",
                table: "NotFile");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "NotFile");

            migrationBuilder.DropColumn(
                name: "ShardCount",
                table: "NotFile");

            migrationBuilder.DropColumn(
                name: "StorageExpiresAt",
                table: "NotFile");

            migrationBuilder.DropColumn(
                name: "StorageUpdatedUtc",
                table: "NotFile");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "NotFile");

            migrationBuilder.DropColumn(
                name: "VolumeId",
                table: "NotFile");
        }
    }
}
