-- Message 服务全量建表脚本（由 EF Core 模型 GenerateCreateScript 生成，2026-08；与 EntityConfig 严格对照）
-- 执行方式: psql -h 127.0.0.1 -U postgres -d messagepostgres -f MessageSchema.sql

CREATE TABLE "ChatSessions" (
    "SessionId" uuid NOT NULL,
    "SessionType" text NOT NULL,
    "SessionName" character varying(200),
    "GroupId" uuid,
    "CreatorId" uuid NOT NULL,
    "Participants" text NOT NULL,
    "LastMessageId" uuid,
    "LastMessageContent" character varying(500),
    "LastMessageTime" timestamp with time zone,
    "CreatedTime" timestamp with time zone NOT NULL,
    "DismissedTime" timestamp with time zone,
    "IsDismissed" boolean NOT NULL,
    "IsPinned" boolean NOT NULL,
    "IsMuted" boolean NOT NULL,
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_ChatSessions" PRIMARY KEY ("SessionId")
);


CREATE TABLE "CircleInvitations" (
    "InviteGuid" uuid NOT NULL,
    "CircleGuid" uuid NOT NULL,
    "InviterGuid" uuid NOT NULL,
    "InviteeGuid" uuid,
    "Code" character varying(6),
    "Token" uuid,
    "Type" text NOT NULL,
    "Status" text NOT NULL,
    "ExpireTime" timestamp with time zone NOT NULL,
    "CreateTime" timestamp with time zone NOT NULL,
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_CircleInvitations" PRIMARY KEY ("InviteGuid")
);


CREATE TABLE "Circles" (
    "CircleGuid" uuid NOT NULL,
    "OwnerGuid" uuid NOT NULL,
    "Name" character varying(50) NOT NULL,
    "Description" character varying(500),
    "AvatarUrl" character varying(500),
    "MaxMembers" integer NOT NULL,
    "MemberCount" integer NOT NULL,
    "Status" text NOT NULL,
    "CreateTime" timestamp with time zone NOT NULL,
    "DissolvedTime" timestamp with time zone,
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_Circles" PRIMARY KEY ("CircleGuid")
);


CREATE TABLE "Comments" (
    "CommentGuid" uuid NOT NULL,
    "TweetGuid" uuid NOT NULL,
    "UserGuid" uuid NOT NULL,
    "ParentGuid" uuid,
    "ReplyToGuid" uuid,
    "Content" character varying(500) NOT NULL,
    "LikeCount" integer NOT NULL,
    "ReplyCount" integer NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "CreateTime" timestamp with time zone NOT NULL,
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_Comments" PRIMARY KEY ("CommentGuid")
);


CREATE TABLE "Groups" (
    "GroupId" uuid NOT NULL,
    "GroupName" character varying(200) NOT NULL,
    "Description" character varying(500),
    "OwnerId" uuid NOT NULL,
    "Avatar" text,
    "MaxMembers" integer NOT NULL,
    "IsPublic" boolean NOT NULL,
    "AllowMemberInvite" boolean NOT NULL,
    "AllowMemberEditInfo" boolean NOT NULL,
    "CreatedTime" timestamp with time zone NOT NULL,
    "DismissedTime" timestamp with time zone,
    "IsDismissed" boolean NOT NULL,
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_Groups" PRIMARY KEY ("GroupId")
);


CREATE TABLE "MessageFriends" (
    "FriendshipId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "FriendId" uuid NOT NULL,
    "Status" text NOT NULL,
    "Remark" character varying(100),
    "FriendGroupName" character varying(50),
    "IsBlocked" boolean NOT NULL,
    "IsMuted" boolean NOT NULL,
    "IsStarred" boolean NOT NULL,
    "CreatedTime" timestamp with time zone NOT NULL,
    "AcceptedTime" timestamp with time zone,
    "LastInteractionTime" timestamp with time zone,
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_MessageFriends" PRIMARY KEY ("FriendshipId")
);


CREATE TABLE "Messages" (
    "MessageId" uuid NOT NULL,
    "SessionId" uuid NOT NULL,
    "SenderId" uuid NOT NULL,
    "ReceiverId" uuid,
    "MessageType" text NOT NULL,
    "Status" text NOT NULL,
    "Content" character varying(4000),
    "MediaUri" character varying(2048),
    "ThumbnailUri" character varying(2048),
    "FileSize" bigint,
    "Duration" double precision,
    "FileName" character varying(500),
    "MimeType" character varying(100),
    "Caption" character varying(500),
    "Latitude" double precision,
    "Longitude" double precision,
    "LocationName" character varying(200),
    "LinkUrl" character varying(2048),
    "LinkTitle" character varying(200),
    "LinkDescription" character varying(500),
    "ExpressionCode" character varying(100),
    "SentTime" timestamp with time zone NOT NULL,
    "DeliveredTime" timestamp with time zone,
    "ReadTime" timestamp with time zone,
    "IsRecalled" boolean NOT NULL,
    "IsEncrypted" boolean NOT NULL,
    "IsForwarded" boolean NOT NULL,
    "OriginalMessageId" uuid,
    "ReplyToMessageId" uuid,
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_Messages" PRIMARY KEY ("MessageId")
);


CREATE TABLE "Topics" (
    "TopicGuid" uuid NOT NULL,
    "Name" character varying(30) NOT NULL,
    "Description" character varying(200),
    "CreatorGuid" uuid NOT NULL,
    "PostCount" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreateTime" timestamp with time zone NOT NULL,
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_Topics" PRIMARY KEY ("TopicGuid")
);


CREATE TABLE "TweetAuditLogs" (
    "AuditGuid" uuid NOT NULL,
    "TweetGuid" uuid NOT NULL,
    "AuditorGuid" uuid NOT NULL,
    "Action" text NOT NULL,
    "Reason" character varying(500) NOT NULL,
    "AuditTime" timestamp with time zone NOT NULL,
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_TweetAuditLogs" PRIMARY KEY ("AuditGuid")
);


CREATE TABLE "TweetInteractions" (
    "Id" uuid NOT NULL,
    "TweetGuid" uuid NOT NULL,
    "UserGuid" uuid NOT NULL,
    "Type" text NOT NULL,
    "CreateTime" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_TweetInteractions" PRIMARY KEY ("Id")
);


CREATE TABLE "TweetNotifications" (
    "Id" uuid NOT NULL,
    "UserGuid" uuid NOT NULL,
    "Type" text NOT NULL,
    "Title" character varying(255) NOT NULL,
    "Content" character varying(2000) NOT NULL,
    "RefType" character varying(20),
    "RefGuid" uuid,
    "IsRead" boolean NOT NULL,
    "CreateTime" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_TweetNotifications" PRIMARY KEY ("Id")
);


CREATE TABLE "TweetReports" (
    "ReportGuid" uuid NOT NULL,
    "ReporterGuid" uuid NOT NULL,
    "TargetType" text NOT NULL,
    "TargetGuid" uuid NOT NULL,
    "ReportedUserGuid" uuid NOT NULL,
    "ReportReason" character varying(1000) NOT NULL,
    "Category" text NOT NULL,
    "Status" text NOT NULL,
    "ReviewerGuid" uuid,
    "ReviewNote" character varying(500),
    "ReviewTime" timestamp with time zone,
    "CreateTime" timestamp with time zone NOT NULL,
    "EvidenceUrls" text NOT NULL,
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_TweetReports" PRIMARY KEY ("ReportGuid")
);


CREATE TABLE "Tweets" (
    "TweetGuid" uuid NOT NULL,
    "AuthorGuid" uuid NOT NULL,
    "Content" character varying(2000) NOT NULL,
    "MediaUrls" text NOT NULL,
    "LinkMetadata" text,
    "Hashtags" text NOT NULL,
    "CircleGuid" uuid,
    "TopicGuids" text NOT NULL,
    "TweetStatus" text NOT NULL,
    "Visibility" text NOT NULL,
    "IsPinned" boolean NOT NULL,
    "ViewCount" bigint NOT NULL,
    "LikeCount" integer NOT NULL,
    "CommentCount" integer NOT NULL,
    "ShareCount" integer NOT NULL,
    "CoinCount" integer NOT NULL,
    "FavoriteCount" integer NOT NULL,
    "HotScore" bigint NOT NULL,
    "AuditReason" character varying(500),
    "PublishTime" timestamp with time zone,
    "CreateTime" timestamp with time zone NOT NULL,
    "UpdateTime" timestamp with time zone NOT NULL,
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_Tweets" PRIMARY KEY ("TweetGuid")
);


CREATE TABLE "UserFollows" (
    "Id" uuid NOT NULL,
    "FollowerGuid" uuid NOT NULL,
    "FolloweeGuid" uuid NOT NULL,
    "CreateTime" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_UserFollows" PRIMARY KEY ("Id")
);


CREATE TABLE "CircleMembers" (
    "Id" uuid NOT NULL,
    "CircleGuid" uuid NOT NULL,
    "UserGuid" uuid NOT NULL,
    "Role" text NOT NULL,
    "Nickname" character varying(50),
    "Status" text NOT NULL,
    "JoinTime" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_CircleMembers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CircleMembers_Circles_CircleGuid" FOREIGN KEY ("CircleGuid") REFERENCES "Circles" ("CircleGuid") ON DELETE CASCADE
);


CREATE TABLE "GroupMembers" (
    "MemberId" uuid NOT NULL,
    "GroupId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Role" text NOT NULL,
    "Nickname" character varying(100),
    "JoinTime" timestamp with time zone NOT NULL,
    "MuteEndTime" timestamp with time zone,
    "IsMuted" boolean NOT NULL,
    "IsBanned" boolean NOT NULL,
    "BannedTime" timestamp with time zone,
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_GroupMembers" PRIMARY KEY ("MemberId"),
    CONSTRAINT "FK_GroupMembers_Groups_GroupId" FOREIGN KEY ("GroupId") REFERENCES "Groups" ("GroupId") ON DELETE CASCADE
);


CREATE TABLE "FileAttachments" (
    "AttachmentId" uuid NOT NULL,
    "MessageId" uuid NOT NULL,
    "FileId" uuid NOT NULL,
    "FileName" character varying(500) NOT NULL,
    "FileType" character varying(100) NOT NULL,
    "FileSize" bigint NOT NULL,
    "FileUri" character varying(2048) NOT NULL,
    "ThumbnailUri" character varying(2048),
    "MimeType" character varying(100),
    "Description" character varying(500),
    "UploadTime" timestamp with time zone NOT NULL,
    "DownloadTime" timestamp with time zone,
    "DownloadCount" integer NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "Id" uuid NOT NULL,
    CONSTRAINT "PK_FileAttachments" PRIMARY KEY ("AttachmentId"),
    CONSTRAINT "FK_FileAttachments_Messages_MessageId" FOREIGN KEY ("MessageId") REFERENCES "Messages" ("MessageId") ON DELETE CASCADE
);


CREATE INDEX "IX_ChatSessions_CreatorId" ON "ChatSessions" ("CreatorId");


CREATE UNIQUE INDEX "IX_ChatSessions_GroupId" ON "ChatSessions" ("GroupId") WHERE "GroupId" IS NOT NULL;


CREATE INDEX "IX_CircleInvitations_CircleGuid" ON "CircleInvitations" ("CircleGuid");


CREATE UNIQUE INDEX "IX_CircleInvitations_Code" ON "CircleInvitations" ("Code");


CREATE INDEX "IX_CircleInvitations_InviteeGuid" ON "CircleInvitations" ("InviteeGuid");


CREATE UNIQUE INDEX "IX_CircleInvitations_Token" ON "CircleInvitations" ("Token");


CREATE UNIQUE INDEX "IX_CircleMembers_CircleGuid_UserGuid" ON "CircleMembers" ("CircleGuid", "UserGuid");


CREATE INDEX "IX_CircleMembers_UserGuid" ON "CircleMembers" ("UserGuid");


CREATE INDEX "IX_Circles_CreateTime" ON "Circles" ("CreateTime" DESC);


CREATE INDEX "IX_Circles_OwnerGuid" ON "Circles" ("OwnerGuid");


CREATE INDEX "IX_Circles_Status" ON "Circles" ("Status");


CREATE INDEX "IX_Comments_ParentGuid" ON "Comments" ("ParentGuid");


CREATE INDEX "IX_Comments_TweetGuid_CreateTime" ON "Comments" ("TweetGuid", "CreateTime");


CREATE INDEX "IX_Comments_UserGuid" ON "Comments" ("UserGuid");


CREATE INDEX "IX_FileAttachments_FileId" ON "FileAttachments" ("FileId");


CREATE INDEX "IX_FileAttachments_MessageId" ON "FileAttachments" ("MessageId");


CREATE UNIQUE INDEX "IX_GroupMembers_GroupId_UserId" ON "GroupMembers" ("GroupId", "UserId");


CREATE INDEX "IX_Groups_IsPublic" ON "Groups" ("IsPublic");


CREATE INDEX "IX_Groups_OwnerId" ON "Groups" ("OwnerId");


CREATE INDEX "IX_MessageFriends_FriendId" ON "MessageFriends" ("FriendId");


CREATE INDEX "IX_MessageFriends_UserId" ON "MessageFriends" ("UserId");


CREATE UNIQUE INDEX "IX_MessageFriends_UserId_FriendId" ON "MessageFriends" ("UserId", "FriendId");


CREATE INDEX "IX_Messages_ReceiverId" ON "Messages" ("ReceiverId");


CREATE INDEX "IX_Messages_SenderId" ON "Messages" ("SenderId");


CREATE INDEX "IX_Messages_SentTime" ON "Messages" ("SentTime");


CREATE INDEX "IX_Messages_SessionId" ON "Messages" ("SessionId");


CREATE UNIQUE INDEX "IX_Topics_Name" ON "Topics" ("Name");


CREATE INDEX "IX_Topics_PostCount" ON "Topics" ("PostCount" DESC);


CREATE INDEX "IX_TweetAuditLogs_AuditorGuid_AuditTime" ON "TweetAuditLogs" ("AuditorGuid", "AuditTime" DESC);


CREATE INDEX "IX_TweetAuditLogs_TweetGuid_AuditTime" ON "TweetAuditLogs" ("TweetGuid", "AuditTime" DESC);


CREATE INDEX "IX_TweetInteractions_TweetGuid" ON "TweetInteractions" ("TweetGuid");


CREATE UNIQUE INDEX "IX_TweetInteractions_TweetGuid_UserGuid_Type" ON "TweetInteractions" ("TweetGuid", "UserGuid", "Type");


CREATE INDEX "IX_TweetInteractions_UserGuid" ON "TweetInteractions" ("UserGuid");


CREATE INDEX "IX_TweetNotifications_Type" ON "TweetNotifications" ("Type");


CREATE INDEX "IX_TweetNotifications_UserGuid_IsRead_CreateTime" ON "TweetNotifications" ("UserGuid", "IsRead", "CreateTime" DESC);


CREATE INDEX "IX_TweetReports_ReporterGuid" ON "TweetReports" ("ReporterGuid");


CREATE INDEX "IX_TweetReports_Status_CreateTime" ON "TweetReports" ("Status", "CreateTime");


CREATE INDEX "IX_TweetReports_TargetType_TargetGuid" ON "TweetReports" ("TargetType", "TargetGuid");


CREATE INDEX "IX_Tweets_AuthorGuid" ON "Tweets" ("AuthorGuid");


CREATE INDEX "IX_Tweets_CircleGuid" ON "Tweets" ("CircleGuid");


CREATE INDEX "IX_Tweets_CreateTime" ON "Tweets" ("CreateTime" DESC);


CREATE INDEX "IX_Tweets_Hashtags" ON "Tweets" ("Hashtags" DESC);


CREATE INDEX "IX_Tweets_HotScore" ON "Tweets" ("HotScore" DESC);


CREATE INDEX "IX_Tweets_TweetStatus_CreateTime" ON "Tweets" ("TweetStatus", "CreateTime" DESC);


CREATE INDEX "IX_UserFollows_FolloweeGuid" ON "UserFollows" ("FolloweeGuid");


CREATE UNIQUE INDEX "IX_UserFollows_FollowerGuid_FolloweeGuid" ON "UserFollows" ("FollowerGuid", "FolloweeGuid");



