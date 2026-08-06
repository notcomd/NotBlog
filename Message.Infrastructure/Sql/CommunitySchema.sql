-- ============================================================================
-- 兴趣社区（Community）建表脚本 — PostgreSQL 17
-- 与 Message.Infrastructure/EntityConfig 一一对应（EF Core ApplyConfigurationsFromAssembly）
-- 数据库: Message 服务连接的 PostgreSQL（NotBlog.AppHost / docker-compose 的 notblog-postgres）
-- 用法: psql -h localhost -U notblog -d <message_db> -f CommunitySchema.sql
-- ============================================================================

-- ── 圈子 ─────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "Circles" (
    "CircleGuid"      uuid NOT NULL,
    "OwnerGuid"       uuid NOT NULL,
    "Name"            varchar(50) NOT NULL,
    "Description"     varchar(500) NULL,
    "AvatarUrl"       varchar(500) NULL,
    "MaxMembers"      integer NOT NULL DEFAULT 500,
    "MemberCount"     integer NOT NULL DEFAULT 0,
    "Status"          varchar(20) NOT NULL DEFAULT 'Active',
    "CreateTime"      timestamptz NOT NULL,
    "DissolvedTime"   timestamptz NULL,
    CONSTRAINT "PK_Circles" PRIMARY KEY ("CircleGuid")
);
CREATE INDEX IF NOT EXISTS "IX_Circles_OwnerGuid"  ON "Circles" ("OwnerGuid");
CREATE INDEX IF NOT EXISTS "IX_Circles_CreateTime" ON "Circles" ("CreateTime" DESC);
CREATE INDEX IF NOT EXISTS "IX_Circles_Status"     ON "Circles" ("Status");

-- ── 圈子成员 ─────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "CircleMembers" (
    "Id"          uuid NOT NULL,
    "CircleGuid"  uuid NOT NULL,
    "UserGuid"    uuid NOT NULL,
    "Role"        varchar(20) NOT NULL,
    "Nickname"    varchar(50) NULL,
    "Status"      varchar(20) NOT NULL DEFAULT 'Active',
    "JoinTime"    timestamptz NOT NULL,
    CONSTRAINT "PK_CircleMembers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CircleMembers_Circles_CircleGuid"
        FOREIGN KEY ("CircleGuid") REFERENCES "Circles" ("CircleGuid") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_CircleMembers_CircleGuid_UserGuid"
    ON "CircleMembers" ("CircleGuid", "UserGuid");
CREATE INDEX IF NOT EXISTS "IX_CircleMembers_UserGuid" ON "CircleMembers" ("UserGuid");

-- ── 圈子邀请（邀请码 / 链接 / 直邀） ─────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "CircleInvitations" (
    "InviteGuid"   uuid NOT NULL,
    "CircleGuid"   uuid NOT NULL,
    "InviterGuid"  uuid NOT NULL,
    "InviteeGuid"  uuid NULL,
    "Code"         varchar(6) NULL,
    "Token"        uuid NULL,
    "Type"         varchar(20) NOT NULL,
    "Status"       varchar(20) NOT NULL DEFAULT 'Pending',
    "ExpireTime"   timestamptz NOT NULL,
    "CreateTime"   timestamptz NOT NULL,
    CONSTRAINT "PK_CircleInvitations" PRIMARY KEY ("InviteGuid"),
    CONSTRAINT "FK_CircleInvitations_Circles_CircleGuid"
        FOREIGN KEY ("CircleGuid") REFERENCES "Circles" ("CircleGuid") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_CircleInvitations_Code"  ON "CircleInvitations" ("Code");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_CircleInvitations_Token" ON "CircleInvitations" ("Token");
CREATE INDEX IF NOT EXISTS "IX_CircleInvitations_CircleGuid"   ON "CircleInvitations" ("CircleGuid");
CREATE INDEX IF NOT EXISTS "IX_CircleInvitations_InviteeGuid"  ON "CircleInvitations" ("InviteeGuid");

-- ── 话题 ─────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "Topics" (
    "TopicGuid"   uuid NOT NULL,
    "Name"        varchar(30) NOT NULL,
    "Description" varchar(200) NULL,
    "CreatorGuid" uuid NOT NULL,
    "PostCount"   integer NOT NULL DEFAULT 0,
    "IsActive"    boolean NOT NULL DEFAULT TRUE,
    "CreateTime"  timestamptz NOT NULL,
    CONSTRAINT "PK_Topics" PRIMARY KEY ("TopicGuid")
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Topics_Name"      ON "Topics" ("Name");
CREATE INDEX IF NOT EXISTS "IX_Topics_PostCount"        ON "Topics" ("PostCount" DESC);

-- ── 关注关系（全局关注 / 粉丝） ──────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "UserFollows" (
    "Id"            uuid NOT NULL,
    "FollowerGuid"  uuid NOT NULL,
    "FolloweeGuid"  uuid NOT NULL,
    "CreateTime"    timestamptz NOT NULL,
    CONSTRAINT "PK_UserFollows" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserFollows_FollowerGuid_FolloweeGuid"
    ON "UserFollows" ("FollowerGuid", "FolloweeGuid");
CREATE INDEX IF NOT EXISTS "IX_UserFollows_FolloweeGuid" ON "UserFollows" ("FolloweeGuid");

-- ── Tweets 表扩展（圈子帖归属 + 话题关联） ───────────────────────────────────
ALTER TABLE "Tweets" ADD COLUMN IF NOT EXISTS "CircleGuid" uuid NULL;
ALTER TABLE "Tweets" ADD COLUMN IF NOT EXISTS "TopicGuids" text NULL DEFAULT '';
CREATE INDEX IF NOT EXISTS "IX_Tweets_CircleGuid" ON "Tweets" ("CircleGuid");
