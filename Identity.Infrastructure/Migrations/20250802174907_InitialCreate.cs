using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Identity");

            migrationBuilder.CreateSequence(
                name: "Author2seq",
                schema: "Identity",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "NotClientseq",
                schema: "Identity",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "RoleClaimseq",
                schema: "Identity",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "Roleseq",
                schema: "Identity",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "UserAccessFailseq",
                schema: "Identity",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "UserSafarseq",
                schema: "Identity",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "Userseq",
                schema: "Identity",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "Author2",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Author2Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorName = table.Column<string>(type: "text", nullable: false),
                    AuthorDescription = table.Column<string>(type: "text", nullable: false),
                    AuthorPrivateKey = table.Column<string>(type: "text", nullable: false),
                    AuthorSecret = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Author2", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotClient",
                schema: "Identity",
                columns: table => new
                {
                    client_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    NotClientName = table.Column<string>(type: "text", nullable: false),
                    NotClientDescription = table.Column<string>(type: "text", nullable: false),
                    NotClientPrivateKey = table.Column<string>(type: "text", nullable: false),
                    NotClientSecret = table.Column<string>(type: "text", nullable: false),
                    NotClientUri = table.Column<string>(type: "text", nullable: false),
                    NotClientType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotClient", x => x.client_guid);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    RoleGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "text", nullable: false),
                    Attribute = table.Column<string>(type: "text", nullable: true),
                    RoleAuthority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoleStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreateRoleTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                schema: "Identity",
                columns: table => new
                {
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserRoleGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImageCover = table.Column<string>(type: "text", nullable: true),
                    UserEmail = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserAddress_Country = table.Column<string>(type: "text", nullable: true),
                    UserAddress_Province = table.Column<string>(type: "text", nullable: true),
                    UserAddress_City = table.Column<string>(type: "text", nullable: true),
                    UserAddress_District = table.Column<string>(type: "text", nullable: true),
                    UserAddress_Street = table.Column<string>(type: "text", nullable: true),
                    UserAddress_Detail = table.Column<string>(type: "text", nullable: true),
                    CreateDateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserGuid);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    RoleGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: false),
                    ClaimValue = table.Column<string>(type: "text", nullable: false),
                    RolesId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Role_RolesId",
                        column: x => x.RolesId,
                        principalSchema: "Identity",
                        principalTable: "Role",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PhoneNumber",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AddressRegion = table.Column<long>(type: "bigint", nullable: false),
                    PhoneCode = table.Column<string>(type: "text", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneNumber", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneNumber_User_UserGuid",
                        column: x => x.UserGuid,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "UserGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAccessFail",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    UserAccessFailGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    LockOutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AccessFaildCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccessFail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccessFail_User_UserGuid",
                        column: x => x.UserGuid,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "UserGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserClaimGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: false),
                    ClaimValue = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_User_UserGuid",
                        column: x => x.UserGuid,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "UserGuid");
                });

            migrationBuilder.CreateTable(
                name: "UserSafety",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    UserSafetyGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    user_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    PasswordSalt = table.Column<string>(type: "text", nullable: true),
                    BlackOrWhite = table.Column<int>(type: "integer", nullable: false),
                    UserStatus = table.Column<int>(type: "integer", nullable: false),
                    LockOutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSafety", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSafety_User_user_guid",
                        column: x => x.user_guid,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "UserGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumber_UserGuid",
                schema: "Identity",
                table: "PhoneNumber",
                column: "UserGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RolesId",
                schema: "Identity",
                table: "RoleClaims",
                column: "RolesId");

            migrationBuilder.CreateIndex(
                name: "IX_User_UserGuid_UserEmail_UserPhone",
                schema: "Identity",
                table: "User",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessFail_UserGuid",
                schema: "Identity",
                table: "UserAccessFail",
                column: "UserGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserGuid",
                schema: "Identity",
                table: "UserClaims",
                column: "UserGuid");

            migrationBuilder.CreateIndex(
                name: "IX_UserSafety_user_guid",
                schema: "Identity",
                table: "UserSafety",
                column: "user_guid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Author2",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "NotClient",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "PhoneNumber",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "RoleClaims",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserAccessFail",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserClaims",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserSafety",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "Role",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "User",
                schema: "Identity");

            migrationBuilder.DropSequence(
                name: "Author2seq",
                schema: "Identity");

            migrationBuilder.DropSequence(
                name: "NotClientseq",
                schema: "Identity");

            migrationBuilder.DropSequence(
                name: "RoleClaimseq",
                schema: "Identity");

            migrationBuilder.DropSequence(
                name: "Roleseq",
                schema: "Identity");

            migrationBuilder.DropSequence(
                name: "UserAccessFailseq",
                schema: "Identity");

            migrationBuilder.DropSequence(
                name: "UserSafarseq",
                schema: "Identity");

            migrationBuilder.DropSequence(
                name: "Userseq",
                schema: "Identity");
        }
    }
}
