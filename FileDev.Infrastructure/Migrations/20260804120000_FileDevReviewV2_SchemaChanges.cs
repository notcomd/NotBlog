using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileDev.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FileDevReviewV2SchemaChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========== NotFileGroup 表变更 ==========

            // 1. 新增 DeleteTime 列（软删除时间记录）
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeleteTime",
                table: "NotFileGroup",
                type: "timestamp with time zone",
                nullable: true);

            // 2. 新增索引 IX_NotFileGroup_UserId
            migrationBuilder.CreateIndex(
                name: "IX_NotFileGroup_UserId",
                table: "NotFileGroup",
                column: "UserId");

            // 3. 新增复合索引 IX_NotFileGroup_UserId_ParentGroupId_FileGroupName
            migrationBuilder.CreateIndex(
                name: "IX_NotFileGroup_UserId_ParentGroupId_FileGroupName",
                table: "NotFileGroup",
                columns: new[] { "UserId", "ParentGroupId", "FileGroupName" });

            // 4. 修改 FileGroupName 列类型：text -> character varying(256)
            migrationBuilder.AlterColumn<string>(
                name: "FileGroupName",
                table: "NotFileGroup",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            // ========== NotFile 表变更 ==========

            // 1. 新增唯一索引 IX_NotFile_FileId
            migrationBuilder.CreateIndex(
                name: "IX_NotFile_FileId",
                table: "NotFile",
                column: "FileId",
                unique: true);

            // 2. 新增索引 IX_NotFile_UserId
            migrationBuilder.CreateIndex(
                name: "IX_NotFile_UserId",
                table: "NotFile",
                column: "UserId");

            // 3. 新增复合索引 IX_NotFile_FileMd5_FileSize
            migrationBuilder.CreateIndex(
                name: "IX_NotFile_FileMd5_FileSize",
                table: "NotFile",
                columns: new[] { "FileMd5", "FileSize" });

            // 4. 新增索引 IX_NotFile_FileUri
            migrationBuilder.CreateIndex(
                name: "IX_NotFile_FileUri",
                table: "NotFile",
                column: "FileUri");

            // 5. 新增复合索引 IX_NotFile_UserId_IsDeleted
            migrationBuilder.CreateIndex(
                name: "IX_NotFile_UserId_IsDeleted",
                table: "NotFile",
                columns: new[] { "UserId", "IsDeleted" });

            // 6. 修改 FileName 列类型：text -> character varying(256)
            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "NotFile",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            // 7. 修改 FileDescription 列类型：text -> character varying(2000)
            migrationBuilder.AlterColumn<string>(
                name: "FileDescription",
                table: "NotFile",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            // 8. 修改 FileMd5 列类型：text -> character varying(128)
            migrationBuilder.AlterColumn<string>(
                name: "FileMd5",
                table: "NotFile",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            // 9. 修改 FileUri 列类型：text -> character varying(1024)
            migrationBuilder.AlterColumn<string>(
                name: "FileUri",
                table: "NotFile",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ========== 回滚 NotFile 表变更 ==========

            // 9. 还原 FileUri 列类型：character varying(1024) -> text
            migrationBuilder.AlterColumn<string>(
                name: "FileUri",
                table: "NotFile",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);

            // 8. 还原 FileMd5 列类型：character varying(128) -> text
            migrationBuilder.AlterColumn<string>(
                name: "FileMd5",
                table: "NotFile",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            // 7. 还原 FileDescription 列类型：character varying(2000) -> text
            migrationBuilder.AlterColumn<string>(
                name: "FileDescription",
                table: "NotFile",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            // 6. 还原 FileName 列类型：character varying(256) -> text
            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "NotFile",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            // 5. 删除索引 IX_NotFile_UserId_IsDeleted
            migrationBuilder.DropIndex(
                name: "IX_NotFile_UserId_IsDeleted",
                table: "NotFile");

            // 4. 删除索引 IX_NotFile_FileUri
            migrationBuilder.DropIndex(
                name: "IX_NotFile_FileUri",
                table: "NotFile");

            // 3. 删除索引 IX_NotFile_FileMd5_FileSize
            migrationBuilder.DropIndex(
                name: "IX_NotFile_FileMd5_FileSize",
                table: "NotFile");

            // 2. 删除索引 IX_NotFile_UserId
            migrationBuilder.DropIndex(
                name: "IX_NotFile_UserId",
                table: "NotFile");

            // 1. 删除索引 IX_NotFile_FileId
            migrationBuilder.DropIndex(
                name: "IX_NotFile_FileId",
                table: "NotFile");

            // ========== 回滚 NotFileGroup 表变更 ==========

            // 4. 还原 FileGroupName 列类型：character varying(256) -> text
            migrationBuilder.AlterColumn<string>(
                name: "FileGroupName",
                table: "NotFileGroup",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            // 3. 删除索引 IX_NotFileGroup_UserId_ParentGroupId_FileGroupName
            migrationBuilder.DropIndex(
                name: "IX_NotFileGroup_UserId_ParentGroupId_FileGroupName",
                table: "NotFileGroup");

            // 2. 删除索引 IX_NotFileGroup_UserId
            migrationBuilder.DropIndex(
                name: "IX_NotFileGroup_UserId",
                table: "NotFileGroup");

            // 1. 删除 DeleteTime 列
            migrationBuilder.DropColumn(
                name: "DeleteTime",
                table: "NotFileGroup");
        }
    }
}
