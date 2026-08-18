using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Message.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MessageDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionType = table.Column<string>(type: "text", nullable: false),
                    SessionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Participants = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    LastMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastMessageContent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastMessageTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DismissedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDismissed = table.Column<bool>(type: "boolean", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    IsMuted = table.Column<bool>(type: "boolean", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "CircleInvitations",
                columns: table => new
                {
                    InviteGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    CircleGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    InviterGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    InviteeGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    Token = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ExpireTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircleInvitations", x => x.InviteGuid);
                });

            migrationBuilder.CreateTable(
                name: "Circles",
                columns: table => new
                {
                    CircleGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CoverUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MaxMembers = table.Column<int>(type: "integer", nullable: false),
                    MemberCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DissolvedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Circles", x => x.CircleGuid);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    CommentGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    TweetGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    ReplyToGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    Content = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    ReplyCount = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.CommentGuid);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Avatar = table.Column<string>(type: "text", nullable: true),
                    MaxMembers = table.Column<int>(type: "integer", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    AllowMemberInvite = table.Column<bool>(type: "boolean", nullable: false),
                    AllowMemberEditInfo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DismissedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDismissed = table.Column<bool>(type: "boolean", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.GroupId);
                });

            migrationBuilder.CreateTable(
                name: "MessageFriends",
                columns: table => new
                {
                    FriendshipId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FriendId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Remark = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FriendGroupName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    IsMuted = table.Column<bool>(type: "boolean", nullable: false),
                    IsStarred = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastInteractionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageFriends", x => x.FriendshipId);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MediaUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ThumbnailUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    Duration = table.Column<double>(type: "double precision", nullable: true),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    LocationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LinkUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    LinkTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LinkDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpressionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRecalled = table.Column<bool>(type: "boolean", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "boolean", nullable: false),
                    IsForwarded = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReplyToMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    TopicGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatorGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    PostCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.TopicGuid);
                });

            migrationBuilder.CreateTable(
                name: "TweetAuditLogs",
                columns: table => new
                {
                    AuditGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    TweetGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditorGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AuditTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TweetAuditLogs", x => x.AuditGuid);
                });

            migrationBuilder.CreateTable(
                name: "TweetInteractions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TweetGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    CreateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TweetInteractions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TweetNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RefType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RefGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TweetNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TweetReports",
                columns: table => new
                {
                    ReportGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "text", nullable: false),
                    TargetGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedUserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReviewerGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReviewTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EvidenceUrls = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TweetReports", x => x.ReportGuid);
                });

            migrationBuilder.CreateTable(
                name: "Tweets",
                columns: table => new
                {
                    TweetGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MediaUrls = table.Column<string>(type: "text", nullable: false),
                    LinkMetadata = table.Column<string>(type: "text", nullable: true),
                    Hashtags = table.Column<string>(type: "text", nullable: false),
                    CircleGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    TopicGuids = table.Column<string>(type: "text", nullable: false),
                    TweetStatus = table.Column<string>(type: "text", nullable: false),
                    Visibility = table.Column<string>(type: "text", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    ViewCount = table.Column<long>(type: "bigint", nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    CommentCount = table.Column<int>(type: "integer", nullable: false),
                    ShareCount = table.Column<int>(type: "integer", nullable: false),
                    CoinCount = table.Column<int>(type: "integer", nullable: false),
                    FavoriteCount = table.Column<int>(type: "integer", nullable: false),
                    HotScore = table.Column<long>(type: "bigint", nullable: false),
                    AuditReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PublishTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tweets", x => x.TweetGuid);
                });

            migrationBuilder.CreateTable(
                name: "UserFollows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowerGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    FolloweeGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    CreateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFollows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserInfos",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    NickName = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Coins = table.Column<long>(type: "bigint", nullable: false),
                    Experience = table.Column<long>(type: "bigint", nullable: false),
                    BackgroundCoverUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    CreateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInfos", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "UserSignIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSignIns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CircleMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CircleGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Nickname = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    JoinTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircleMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CircleMembers_Circles_CircleGuid",
                        column: x => x.CircleGuid,
                        principalTable: "Circles",
                        principalColumn: "CircleGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                columns: table => new
                {
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Nickname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    JoinTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MuteEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsMuted = table.Column<bool>(type: "boolean", nullable: false),
                    IsBanned = table.Column<bool>(type: "boolean", nullable: false),
                    BannedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => x.MemberId);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileAttachments",
                columns: table => new
                {
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ThumbnailUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UploadTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DownloadTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DownloadCount = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAttachments", x => x.AttachmentId);
                    table.ForeignKey(
                        name: "FK_FileAttachments_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "MessageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_CreatorId",
                table: "ChatSessions",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_GroupId",
                table: "ChatSessions",
                column: "GroupId",
                unique: true,
                filter: "\"GroupId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CircleInvitations_CircleGuid",
                table: "CircleInvitations",
                column: "CircleGuid");

            migrationBuilder.CreateIndex(
                name: "IX_CircleInvitations_Code",
                table: "CircleInvitations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CircleInvitations_InviteeGuid",
                table: "CircleInvitations",
                column: "InviteeGuid");

            migrationBuilder.CreateIndex(
                name: "IX_CircleInvitations_Token",
                table: "CircleInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CircleMembers_CircleGuid_UserGuid",
                table: "CircleMembers",
                columns: new[] { "CircleGuid", "UserGuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CircleMembers_UserGuid",
                table: "CircleMembers",
                column: "UserGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Circles_CreateTime",
                table: "Circles",
                column: "CreateTime",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Circles_OwnerGuid",
                table: "Circles",
                column: "OwnerGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Circles_Status",
                table: "Circles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentGuid",
                table: "Comments",
                column: "ParentGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_TweetGuid_CreateTime",
                table: "Comments",
                columns: new[] { "TweetGuid", "CreateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserGuid",
                table: "Comments",
                column: "UserGuid");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_FileId",
                table: "FileAttachments",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_MessageId",
                table: "FileAttachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId_UserId",
                table: "GroupMembers",
                columns: new[] { "GroupId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_IsPublic",
                table: "Groups",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OwnerId",
                table: "Groups",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageFriends_FriendId",
                table: "MessageFriends",
                column: "FriendId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageFriends_UserId",
                table: "MessageFriends",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageFriends_UserId_FriendId",
                table: "MessageFriends",
                columns: new[] { "UserId", "FriendId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReceiverId",
                table: "Messages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SentTime",
                table: "Messages",
                column: "SentTime");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SessionId",
                table: "Messages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Name",
                table: "Topics",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Topics_PostCount",
                table: "Topics",
                column: "PostCount",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_TweetAuditLogs_AuditorGuid_AuditTime",
                table: "TweetAuditLogs",
                columns: new[] { "AuditorGuid", "AuditTime" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_TweetAuditLogs_TweetGuid_AuditTime",
                table: "TweetAuditLogs",
                columns: new[] { "TweetGuid", "AuditTime" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_TweetInteractions_TweetGuid",
                table: "TweetInteractions",
                column: "TweetGuid");

            migrationBuilder.CreateIndex(
                name: "IX_TweetInteractions_TweetGuid_UserGuid_Type",
                table: "TweetInteractions",
                columns: new[] { "TweetGuid", "UserGuid", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TweetInteractions_UserGuid",
                table: "TweetInteractions",
                column: "UserGuid");

            migrationBuilder.CreateIndex(
                name: "IX_TweetNotifications_Type",
                table: "TweetNotifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_TweetNotifications_UserGuid_IsRead_CreateTime",
                table: "TweetNotifications",
                columns: new[] { "UserGuid", "IsRead", "CreateTime" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_TweetReports_ReporterGuid",
                table: "TweetReports",
                column: "ReporterGuid");

            migrationBuilder.CreateIndex(
                name: "IX_TweetReports_Status_CreateTime",
                table: "TweetReports",
                columns: new[] { "Status", "CreateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TweetReports_TargetType_TargetGuid",
                table: "TweetReports",
                columns: new[] { "TargetType", "TargetGuid" });

            migrationBuilder.CreateIndex(
                name: "IX_Tweets_AuthorGuid",
                table: "Tweets",
                column: "AuthorGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Tweets_CircleGuid",
                table: "Tweets",
                column: "CircleGuid");

            migrationBuilder.CreateIndex(
                name: "IX_Tweets_CreateTime",
                table: "Tweets",
                column: "CreateTime",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Tweets_Hashtags",
                table: "Tweets",
                column: "Hashtags",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Tweets_HotScore",
                table: "Tweets",
                column: "HotScore",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Tweets_TweetStatus_CreateTime",
                table: "Tweets",
                columns: new[] { "TweetStatus", "CreateTime" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_UserFollows_FolloweeGuid",
                table: "UserFollows",
                column: "FolloweeGuid");

            migrationBuilder.CreateIndex(
                name: "IX_UserFollows_FollowerGuid_FolloweeGuid",
                table: "UserFollows",
                columns: new[] { "FollowerGuid", "FolloweeGuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSignIns_UserId",
                table: "UserSignIns",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSignIns_UserId_SignInDate",
                table: "UserSignIns",
                columns: new[] { "UserId", "SignInDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.DropTable(
                name: "CircleInvitations");

            migrationBuilder.DropTable(
                name: "CircleMembers");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "FileAttachments");

            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "MessageFriends");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "TweetAuditLogs");

            migrationBuilder.DropTable(
                name: "TweetInteractions");

            migrationBuilder.DropTable(
                name: "TweetNotifications");

            migrationBuilder.DropTable(
                name: "TweetReports");

            migrationBuilder.DropTable(
                name: "Tweets");

            migrationBuilder.DropTable(
                name: "UserFollows");

            migrationBuilder.DropTable(
                name: "UserInfos");

            migrationBuilder.DropTable(
                name: "UserSignIns");

            migrationBuilder.DropTable(
                name: "Circles");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Groups");
        }
    }
}
