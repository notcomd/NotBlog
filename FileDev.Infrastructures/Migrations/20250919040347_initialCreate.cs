using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileDev.Infrastructres.Migrations
{
    /// <inheritdoc />
    public partial class initialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotFileRepository",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    NotFileRepositoryName = table.Column<string>(type: "text", nullable: false),
                    FileSafety = table.Column<int>(type: "integer", nullable: false),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FileCount = table.Column<long>(type: "bigint", nullable: false),
                    RepositoryBrief = table.Column<string>(type: "text", nullable: true),
                    RepositoryCover = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotFileRepository", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    FileGroupName = table.Column<string>(type: "text", nullable: false),
                    FileSafety = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    MaterializedPath = table.Column<string>(type: "text", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileGroupTags = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileGroup_FileGroup_ParentId",
                        column: x => x.ParentId,
                        principalTable: "FileGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileGroup_NotFileRepository_RepositoryGuid",
                        column: x => x.RepositoryGuid,
                        principalTable: "NotFileRepository",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotFile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileGroupGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileDescription = table.Column<string>(type: "text", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<double>(type: "double precision", nullable: false),
                    FileSafety = table.Column<int>(type: "integer", nullable: false),
                    FileHash = table.Column<string>(type: "text", nullable: false),
                    ObjectKey = table.Column<string>(type: "varchar", nullable: false),
                    PhysicalName = table.Column<string>(type: "varchar", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotFile_FileGroup_FileGroupGuid",
                        column: x => x.FileGroupGuid,
                        principalTable: "FileGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileGroup_ParentId",
                table: "FileGroup",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_FileGroup_RepositoryGuid",
                table: "FileGroup",
                column: "RepositoryGuid");

            migrationBuilder.CreateIndex(
                name: "IX_NotFile_FileGroupGuid",
                table: "NotFile",
                column: "FileGroupGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotFile");

            migrationBuilder.DropTable(
                name: "FileGroup");

            migrationBuilder.DropTable(
                name: "NotFileRepository");
        }
    }
}
