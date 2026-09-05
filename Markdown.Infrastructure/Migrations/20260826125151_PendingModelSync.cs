using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Markdown.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MarkReview",
                table: "MarkReview");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MarkFavoriteTag",
                table: "MarkFavoriteTag");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "MarkReview");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "MarkFavoriteTag");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MarkReview",
                table: "MarkReview",
                column: "MarkDownGuid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MarkFavoriteTag",
                table: "MarkFavoriteTag",
                column: "MarkFavoriteTagGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MarkReview",
                table: "MarkReview");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MarkFavoriteTag",
                table: "MarkFavoriteTag");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "MarkReview",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "MarkFavoriteTag",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_MarkReview",
                table: "MarkReview",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MarkFavoriteTag",
                table: "MarkFavoriteTag",
                column: "Id");
        }
    }
}
