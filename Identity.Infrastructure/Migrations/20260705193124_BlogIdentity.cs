using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BlogIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "NotClientseq",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "UserAccessFailseq",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "UserSafarseq",
                incrementBy: 10);

            migrationBuilder.CreateSequence(
                name: "Userseq",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "NotClient",
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
                name: "Permission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionCode = table.Column<string>(type: "text", nullable: false),
                    PermissionName = table.Column<string>(type: "text", nullable: false),
                    PermissionType = table.Column<string>(type: "text", nullable: false),
                    MenuPath = table.Column<string>(type: "text", nullable: true),
                    ApiMethod = table.Column<string>(type: "text", nullable: true),
                    ApiUrl = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleGroups",
                columns: table => new
                {
                    RoleGroupGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    RoleGroupName = table.Column<string>(type: "text", nullable: false),
                    RoleGroupCode = table.Column<string>(type: "text", nullable: false),
                    CreatedRoleGroup = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleGroups", x => x.RoleGroupGuid);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    RoleName = table.Column<string>(type: "text", nullable: false),
                    Attribute = table.Column<string>(type: "text", nullable: true),
                    RoleCode = table.Column<string>(type: "text", nullable: false),
                    RoleAuthority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoleStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreateRole = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleGuid);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    user_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    user_role_guid = table.Column<HashSet<Guid>>(type: "uuid[]", nullable: false),
                    AuthorGuids = table.Column<HashSet<Guid>>(type: "uuid[]", nullable: false),
                    user_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    image_cover = table.Column<string>(type: "text", nullable: true),
                    UserEmail = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserAddress_Country = table.Column<string>(type: "text", nullable: true),
                    UserAddress_Province = table.Column<string>(type: "text", nullable: true),
                    UserAddress_City = table.Column<string>(type: "text", nullable: true),
                    UserAddress_District = table.Column<string>(type: "text", nullable: true),
                    UserAddress_Street = table.Column<string>(type: "text", nullable: true),
                    UserAddress_Detail = table.Column<string>(type: "text", nullable: true),
                    create_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.user_guid);
                });

            migrationBuilder.CreateTable(
                name: "UserExternalLogins",
                columns: table => new
                {
                    LoginId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ProviderUnionId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ProviderDisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EncryptedAccessToken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EncryptedRefreshToken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExternalLogins", x => x.LoginId);
                });

            migrationBuilder.CreateTable(
                name: "GroupPermissions",
                columns: table => new
                {
                    PermissionGuid = table.Column<int>(type: "integer", nullable: false),
                    RoleGroupGuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPermissions", x => new { x.PermissionGuid, x.RoleGroupGuid });
                    table.ForeignKey(
                        name: "FK_GroupPermissions_Permission_PermissionGuid",
                        column: x => x.PermissionGuid,
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupPermissions_RoleGroups_RoleGroupGuid",
                        column: x => x.RoleGroupGuid,
                        principalTable: "RoleGroups",
                        principalColumn: "RoleGroupGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleGroupRoles",
                columns: table => new
                {
                    RoleGroupGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleGuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleGroupRoles", x => new { x.RoleGroupGuid, x.RoleGuid });
                    table.ForeignKey(
                        name: "FK_RoleGroupRoles_RoleGroups_RoleGroupGuid",
                        column: x => x.RoleGroupGuid,
                        principalTable: "RoleGroups",
                        principalColumn: "RoleGroupGuid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleGroupRoles_Roles_RoleGuid",
                        column: x => x.RoleGuid,
                        principalTable: "Roles",
                        principalColumn: "RoleGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    PermissionGuid = table.Column<int>(type: "integer", nullable: false),
                    RoleGuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.PermissionGuid, x.RoleGuid });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permission_PermissionGuid",
                        column: x => x.PermissionGuid,
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleGuid",
                        column: x => x.RoleGuid,
                        principalTable: "Roles",
                        principalColumn: "RoleGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhoneNumber",
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
                        principalTable: "User",
                        principalColumn: "user_guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAccessFail",
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
                        principalTable: "User",
                        principalColumn: "user_guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSafety",
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
                        principalTable: "User",
                        principalColumn: "user_guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupPermissions_RoleGroupGuid",
                table: "GroupPermissions",
                column: "RoleGroupGuid");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumber_UserGuid",
                table: "PhoneNumber",
                column: "UserGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleGroupRoles_RoleGuid",
                table: "RoleGroupRoles",
                column: "RoleGuid");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleGuid",
                table: "RolePermissions",
                column: "RoleGuid");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessFail_UserGuid",
                table: "UserAccessFail",
                column: "UserGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserExternalLogins_Provider_ProviderKey",
                table: "UserExternalLogins",
                columns: new[] { "Provider", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserExternalLogins_UserId",
                table: "UserExternalLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSafety_user_guid",
                table: "UserSafety",
                column: "user_guid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupPermissions");

            migrationBuilder.DropTable(
                name: "NotClient");

            migrationBuilder.DropTable(
                name: "PhoneNumber");

            migrationBuilder.DropTable(
                name: "RoleGroupRoles");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserAccessFail");

            migrationBuilder.DropTable(
                name: "UserExternalLogins");

            migrationBuilder.DropTable(
                name: "UserSafety");

            migrationBuilder.DropTable(
                name: "RoleGroups");

            migrationBuilder.DropTable(
                name: "Permission");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropSequence(
                name: "NotClientseq");

            migrationBuilder.DropSequence(
                name: "UserAccessFailseq");

            migrationBuilder.DropSequence(
                name: "UserSafarseq");

            migrationBuilder.DropSequence(
                name: "Userseq");
        }
    }
}
