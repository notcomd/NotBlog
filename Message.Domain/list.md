# Message.Domain 项目任务清单

## 项目概述

即时通讯系统领域层设计，支持用户聊天、群聊、消息撤回、消息转发、已读回执、离线消息存储、消息加密等功能。

---

## 任务列表

| 任务ID | 任务描述               | 负责人          | 开始时间       | 预计完成时间     | 当前进度状态 | 完成情况说明                                                                                                                                                                                                                                                                                                          | 备注                                                                 |
|------|--------------------|--------------|------------|------------|--------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------|
| T001 | 设计枚举类型             | AI Assistant | 2026-03-09 | 2026-03-09 | ✅ 已完成  | 已创建MessageType、UserStatus、SessionType、MessageStatus、FriendshipStatus、GroupMemberRole、EncryptionAlgorithm、ForwardType、RecallReason枚举                                                                                                                                                                             | -                                                                  |
| T002 | 设计群聊功能实体           | AI Assistant | 2026-03-09 | 2026-03-09 | ✅ 已完成  | 已创建Group、GroupMember实体，支持群聊创建、成员管理、权限控制、群解散等功能                                                                                                                                                                                                                                                                  | 包含群主、管理员、成员三种角色                                                    |
| T003 | 设计消息撤回机制实体         | AI Assistant | 2026-03-09 | 2026-03-09 | ✅ 已完成  | 已创建MessageRecall实体，支持2分钟内撤回、撤回原因记录                                                                                                                                                                                                                                                                              | 默认撤回时限为2分钟                                                         |
| T004 | 设计消息转发功能实体         | AI Assistant | 2026-03-09 | 2026-03-09 | ✅ 已完成  | 已创建MessageForward实体，支持直接转发和引用转发，记录转发链路                                                                                                                                                                                                                                                                          | 支持转发来源追溯                                                           |
| T005 | 设计消息已读回执实体         | AI Assistant | 2026-03-09 | 2026-03-09 | ✅ 已完成  | 已创建MessageReadReceipt和GroupReadReceipt实体，支持单聊和群聊已读状态跟踪                                                                                                                                                                                                                                                          | 群聊支持已读用户列表                                                         |
| T006 | 设计离线消息存储实体         | AI Assistant | 2026-03-09 | 2026-03-09 | ✅ 已完成  | 已创建OfflineMessage、OfflineMessageConfig、UserOfflineStorage实体，存储时长3天，空间上限1024MB                                                                                                                                                                                                                                   | 支持过期自动清理                                                           |
| T007 | 设计消息加密功能实体         | AI Assistant | 2026-03-09 | 2026-03-09 | ✅ 已完成  | 已创建MessageEncryption、EncryptionKey、SessionKey实体，支持AES-256-GCM、RSA-2048、ChaCha20-Poly1305加密算法                                                                                                                                                                                                                    | 包含密钥管理功能                                                           |
| T008 | 重构User实体           | AI Assistant | 2026-03-09 | 2026-03-09 | ✅ 已完成  | 已重构User实体，添加在线状态、头像、最后在线时间等属性                                                                                                                                                                                                                                                                                   | 支持用户状态管理                                                           |
| T009 | 重构Message实体        | AI Assistant | 2026-03-09 | 2026-03-09 | ✅ 已完成  | 已重构Message实体，支持文本、图片、视频、音频、文件、位置、链接、表情等多种消息类型                                                                                                                                                                                                                                                                   | 包含消息状态流转、撤回、转发、加密等功能                                               |
| T010 | 创建ChatSession实体    | AI Assistant | 2026-03-09 | 2026-03-09 | ✅ 已完成  | 已创建ChatSession实体，支持私聊和群聊会话管理，包含参与者、未读计数、最后消息等功能                                                                                                                                                                                                                                                                 | 支持会话置顶、静音                                                          |
| T011 | 创建FileAttachment实体 | AI Assistant | 2026-03-09 | 2026-03-09 | ✅ 已完成  | 已创建FileAttachment实体，支持文件附件管理，包含文件名、类型、大小、URI等属性                                                                                                                                                                                                                                                                 | 支持下载计数、文件类型判断                                                      |
| T012 | 重构MessageFriends实体 | AI Assistant | 2026-03-09 | 2026-03-09 | ✅ 已完成  | 已重构MessageFriends实体，支持好友关系管理，包含好友状态、备注、分组、屏蔽、静音等功能                                                                                                                                                                                                                                                              | 支持好友请求接受/拒绝                                                        |
| T013 | 创建领域事件             | AI Assistant | 2026-03-09 | 2026-03-09 | ✅ 已完成  | 已创建MessageSentEvent、MessageReceivedEvent、MessageReadEvent、MessageRecalledEvent、MessageForwardedEvent、FileUploadedEvent、FileDownloadedEvent、UserOnlineEvent、UserOfflineEvent、FriendshipCreatedEvent、FriendshipAcceptedEvent、SessionCreatedEvent、GroupCreatedEvent、GroupMemberJoinedEvent、GroupMemberLeftEvent等事件 | 使用NotMediator实现                                                    |
| T014 | 创建值对象              | AI Assistant | -          | -          | ⏳ 未开始  | -                                                                                                                                                                                                                                                                                                               | 需要创建FileSize、MessageContent等值对象                                    |
| T015 | 创建仓储接口             | AI Assistant | -          | -          | ⏳ 未开始  | -                                                                                                                                                                                                                                                                                                               | 需要创建IMessageRepository、IChatSessionRepository、IUserRepository等仓储接口 |
| T016 | 创建领域服务             | AI Assistant | -          | -          | ⏳ 未开始  | -                                                                                                                                                                                                                                                                                                               | 需要创建MessageService、ChatSessionService、GroupService等领域服务            |

---

## 实体关系图

```
User (用户)
  ├── MessageFriends (好友关系)
  ├── ChatSession (会话)
  │     └── Message (消息)
  │           ├── FileAttachment (附件)
  │           ├── MessageRecall (撤回记录)
  │           ├── MessageForward (转发记录)
  │           ├── MessageReadReceipt (已读回执)
  │           └── MessageEncryption (加密信息)
  └── Group (群聊)
        └── GroupMember (群成员)
              └── GroupRole (群角色)

OfflineMessage (离线消息)
  └── UserOfflineStorage (用户存储空间)

EncryptionKey (加密密钥)
  └── SessionKey (会话密钥)
```

---

## 技术选型

- **框架**: .NET 9.0
- **领域事件**: NotMediator (v1.0.5)
- **依赖注入**: Microsoft.Extensions.DependencyInjection.Abstractions (v10.0.4)
- **配置**: Microsoft.Extensions.Configuration.Abstractions (v10.0.4)

---

## 注意事项

1. 所有实体都遵循DDD原则，使用领域事件进行解耦
2. 消息撤回默认时限为2分钟
3. 离线消息存储时长为3天，空间上限为1024MB
4. 群聊支持三种角色：群主、管理员、成员
5. 消息加密支持三种算法：AES-256-GCM、RSA-2048、ChaCha20-Poly1305

---

## 更新记录

| 日期         | 更新内容      | 更新人          |
|------------|-----------|--------------|
| 2026-03-09 | 初始化项目任务清单 | AI Assistant |
| 2026-03-09 | 完成所有实体设计  | AI Assistant |
| 2026-03-09 | 完成领域事件创建  | AI Assistant |
