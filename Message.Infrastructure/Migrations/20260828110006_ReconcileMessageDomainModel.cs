using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Message.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileMessageDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Caption",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ExpressionCode",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "LinkDescription",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "LinkTitle",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "LinkUrl",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MediaUri",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MimeType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ThumbnailUri",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "MessageFriends");

            migrationBuilder.DropColumn(
                name: "IsMuted",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "SessionName",
                table: "ChatSessions");

            migrationBuilder.AddColumn<Guid>(
                name: "CircleMembleGuid",
                table: "CircleMembers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CircleMembleGuid",
                table: "CircleMembers");

            migrationBuilder.AddColumn<string>(
                name: "Caption",
                table: "Messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Messages",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Duration",
                table: "Messages",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpressionCode",
                table: "Messages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "Messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Messages",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkDescription",
                table: "Messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkTitle",
                table: "Messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkUrl",
                table: "Messages",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "Messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Messages",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaUri",
                table: "Messages",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageType",
                table: "Messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "Messages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUri",
                table: "Messages",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "MessageFriends",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMuted",
                table: "ChatSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "ChatSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SessionName",
                table: "ChatSessions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
