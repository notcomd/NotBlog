using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileDev.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_NotFile",
                table: "NotFile");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "NotFile");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotFile",
                table: "NotFile",
                column: "FileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_NotFile",
                table: "NotFile");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "NotFile",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotFile",
                table: "NotFile",
                column: "Id");
        }
    }
}
