using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Message.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSessionCircleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CircleId",
                table: "ChatSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_CircleId",
                table: "ChatSessions",
                column: "CircleId",
                unique: true,
                filter: "\"CircleId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_CircleId",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "CircleId",
                table: "ChatSessions");
        }
    }
}
