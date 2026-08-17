using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class identity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientRequest",
                columns: table => new
                {
                    ClientRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientRequestName = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientRequest", x => x.ClientRequestId);
                });

            migrationBuilder.CreateTable(
                name: "NotClient",
                columns: table => new
                {
                    client_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    application_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    application_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    application_icon = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    homepage_uri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    privacy_policy_uri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    terms_of_service_uri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    client_secret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    redirect_uris = table.Column<HashSet<string>>(type: "text[]", nullable: false),
                    post_logout_redirect_uris = table.Column<HashSet<string>>(type: "text[]", nullable: false),
                    allowed_scopes = table.Column<HashSet<string>>(type: "text[]", nullable: false),
                    allowed_grant_types = table.Column<HashSet<string>>(type: "text[]", nullable: false),
                    application_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    token_endpoint_auth_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    require_pkce = table.Column<bool>(type: "boolean", nullable: false),
                    require_consent = table.Column<bool>(type: "boolean", nullable: false),
                    allowed_cors_origins = table.Column<HashSet<string>>(type: "text[]", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotClient", x => x.client_guid);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventData = table.Column<string>(type: "jsonb", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ProcessCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
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
                    table.PrimaryKey("PK_Permissions", x => x.PermissionId);
                });

            migrationBuilder.CreateTable(
                name: "RoleGroups",
                columns: table => new
                {
                    RoleGroupGuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    RoleGroupName = table.Column<string>(type: "text", nullable: false),
                    RoleGroupCode = table.Column<string>(type: "text", nullable: false),
                    role_guids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    CreatedRoleGroup = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                    UserGuid = table.Column<HashSet<Guid>>(type: "uuid[]", nullable: false),
                    RoleName = table.Column<string>(type: "text", nullable: false),
                    Attribute = table.Column<string>(type: "text", nullable: true),
                    RoleCode = table.Column<string>(type: "text", nullable: false),
                    RoleAuthority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoleStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreateRole = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    role_group_guids = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
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
                    user_role_guid = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    AuthorGuids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    user_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    UserEmail = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhoneNumber_AddressRegion = table.Column<long>(type: "bigint", nullable: true),
                    PhoneNumber_PhoneCode = table.Column<string>(type: "text", nullable: true),
                    UserAddress_Country = table.Column<string>(type: "text", nullable: true),
                    UserAddress_Province = table.Column<string>(type: "text", nullable: true),
                    UserAddress_City = table.Column<string>(type: "text", nullable: true),
                    UserAddress_District = table.Column<string>(type: "text", nullable: true),
                    UserAddress_Street = table.Column<string>(type: "text", nullable: true),
                    UserAddress_Detail = table.Column<string>(type: "text", nullable: true),
                    create_datetime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "UserLoginHistory",
                columns: table => new
                {
                    LoginGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneNumber_AddressRegion = table.Column<long>(type: "bigint", nullable: false),
                    PhoneNumber_PhoneCode = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    CreateDataTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LoginMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLoginHistory", x => x.LoginGuid);
                });

            migrationBuilder.CreateTable(
                name: "RoleGroupPermissions",
                columns: table => new
                {
                    PermissionGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleGroupGuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleGroupPermissions", x => new { x.PermissionGuid, x.RoleGroupGuid });
                    table.ForeignKey(
                        name: "FK_RoleGroupPermissions_Permissions_PermissionGuid",
                        column: x => x.PermissionGuid,
                        principalTable: "Permissions",
                        principalColumn: "PermissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleGroupPermissions_RoleGroups_RoleGroupGuid",
                        column: x => x.RoleGroupGuid,
                        principalTable: "RoleGroups",
                        principalColumn: "RoleGroupGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    PermissionGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleGuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.PermissionGuid, x.RoleGuid });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionGuid",
                        column: x => x.PermissionGuid,
                        principalTable: "Permissions",
                        principalColumn: "PermissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleGuid",
                        column: x => x.RoleGuid,
                        principalTable: "Roles",
                        principalColumn: "RoleGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAccessFail",
                columns: table => new
                {
                    UserAccessFailGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    LockOutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AccessFaildCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccessFail", x => x.UserAccessFailGuid);
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
                    table.PrimaryKey("PK_UserSafety", x => x.UserSafetyGuid);
                    table.ForeignKey(
                        name: "FK_UserSafety_User_user_guid",
                        column: x => x.user_guid,
                        principalTable: "User",
                        principalColumn: "user_guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotClient_client_id",
                table: "NotClient",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_SentAt",
                table: "OutboxMessages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status_CreatedAt",
                table: "OutboxMessages",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleGroupPermissions_RoleGroupGuid",
                table: "RoleGroupPermissions",
                column: "RoleGroupGuid");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleGuid",
                table: "RolePermissions",
                column: "RoleGuid");

            migrationBuilder.CreateIndex(
                name: "IX_User_UserEmail",
                table: "User",
                column: "UserEmail",
                unique: true);

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
                name: "ClientRequest");

            migrationBuilder.DropTable(
                name: "NotClient");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "RoleGroupPermissions");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserAccessFail");

            migrationBuilder.DropTable(
                name: "UserExternalLogins");

            migrationBuilder.DropTable(
                name: "UserLoginHistory");

            migrationBuilder.DropTable(
                name: "UserSafety");

            migrationBuilder.DropTable(
                name: "RoleGroups");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
