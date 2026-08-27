using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileDev.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotFileTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotFileGroup");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotFileTag");

            migrationBuilder.CreateTable(
                name: "NotFileGroup",
                columns: table => new
                {
                    NotFileGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeleteTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Depth = table.Column<int>(type: "integer", nullable: false),
                    FileGroupDescription = table.Column<string>(type: "text", nullable: true),
                    FileGroupName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FileGroupTags = table.Column<string>(type: "text", nullable: false),
                    FileIdentity = table.Column<int>(type: "integer", nullable: false),
                    FileIds = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UploadTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotFileGroup", x => x.NotFileGroupId);
                    table.ForeignKey(
                        name: "FK_NotFileGroup_NotFileGroup_ParentGroupId",
                        column: x => x.ParentGroupId,
                        principalTable: "NotFileGroup",
                        principalColumn: "NotFileGroupId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotFileGroup_ParentGroupId",
                table: "NotFileGroup",
                column: "ParentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_NotFileGroup_UserId",
                table: "NotFileGroup",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotFileGroup_UserId_ParentGroupId_FileGroupName",
                table: "NotFileGroup",
                columns: new[] { "UserId", "ParentGroupId", "FileGroupName" });
        }
    }
}
