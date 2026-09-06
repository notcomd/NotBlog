using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PermissionTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiMethod",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "ApiUrl",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "MenuPath",
                table: "Permissions");

            migrationBuilder.AlterColumn<string>(
                name: "PermissionType",
                table: "Permissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PermissionName",
                table: "Permissions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PermissionCode",
                table: "Permissions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Permissions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "Permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Permissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Permissions",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_ParentId",
                table: "Permissions",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Permissions_ParentId",
                table: "Permissions",
                column: "ParentId",
                principalTable: "Permissions",
                principalColumn: "PermissionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Permissions_ParentId",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_ParentId",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Permissions");

            migrationBuilder.AlterColumn<string>(
                name: "PermissionType",
                table: "Permissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PermissionName",
                table: "Permissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "PermissionCode",
                table: "Permissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AddColumn<string>(
                name: "ApiMethod",
                table: "Permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiUrl",
                table: "Permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MenuPath",
                table: "Permissions",
                type: "text",
                nullable: true);
        }
    }
}
