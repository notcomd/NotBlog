using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Notcomd.Identity.Server.Migrations.Notcomd_Identity_User_Image_Db
{
    /// <inheritdoc />
    public partial class RmakeUserImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notcomd_User_Supplement_Module",
                columns: table => new
                {
                    WeChat_Numble = table.Column<string>(type: "text", nullable: false),
                    UserLin_Status = table.Column<bool>(type: "boolean", nullable: false),
                    UserTag = table.Column<List<string>>(type: "text[]", nullable: false),
                    UserRemake = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notcomd_User_Supplement_Module", x => x.WeChat_Numble);
                });

            migrationBuilder.CreateTable(
                name: "UserImageTable",
                columns: table => new
                {
                    Identity_UserName = table.Column<string>(type: "text", nullable: false),
                    Identity_ImageUrl = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserImageTable", x => x.Identity_UserName);
                });

            migrationBuilder.CreateTable(
                name: "Notcomd_User_Module",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Notcomd_User_Supplement_ModuleWeChat_Numble = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "text", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notcomd_User_Module", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notcomd_User_Module_Notcomd_User_Supplement_Module_Notcomd_~",
                        column: x => x.Notcomd_User_Supplement_ModuleWeChat_Numble,
                        principalTable: "Notcomd_User_Supplement_Module",
                        principalColumn: "WeChat_Numble");
                    table.ForeignKey(
                        name: "FK_Notcomd_User_Module_UserImageTable_Email",
                        column: x => x.Email,
                        principalTable: "UserImageTable",
                        principalColumn: "Identity_UserName");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notcomd_User_Module_Email",
                table: "Notcomd_User_Module",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Notcomd_User_Module_Notcomd_User_Supplement_ModuleWeChat_Nu~",
                table: "Notcomd_User_Module",
                column: "Notcomd_User_Supplement_ModuleWeChat_Numble");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notcomd_User_Module");

            migrationBuilder.DropTable(
                name: "Notcomd_User_Supplement_Module");

            migrationBuilder.DropTable(
                name: "UserImageTable");
        }
    }
}
