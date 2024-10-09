using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class User : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhoneNumber",
                columns: table => new
                {
                    PhoneCode = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    AddressRegion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneNumber", x => x.PhoneCode);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserRoleGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    UserEmail = table.Column<string>(type: "text", nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    UserPhonePhoneCode = table.Column<string>(type: "character varying(11)", nullable: true),
                    UserAddress = table.Column<string>(type: "text", nullable: true),
                    CreateDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BlackOrWhite = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserGuid);
                    table.ForeignKey(
                        name: "FK_Users_PhoneNumber_UserPhonePhoneCode",
                        column: x => x.UserPhonePhoneCode,
                        principalTable: "PhoneNumber",
                        principalColumn: "PhoneCode");
                });

            migrationBuilder.CreateTable(
                name: "UserAccessFail",
                columns: table => new
                {
                    UserAccessFailGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    LockOutEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AccessFaildCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccessFail", x => x.UserAccessFailGuid);
                    table.ForeignKey(
                        name: "FK_UserAccessFail_Users_UserGuid",
                        column: x => x.UserGuid,
                        principalTable: "Users",
                        principalColumn: "UserGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessFail_UserGuid",
                table: "UserAccessFail",
                column: "UserGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserPhonePhoneCode",
                table: "Users",
                column: "UserPhonePhoneCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAccessFail");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "PhoneNumber");
        }
    }
}
