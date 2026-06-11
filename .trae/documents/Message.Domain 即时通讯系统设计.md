
# 即时通讯系统实体设计方案

## 一、核心实体重构

### 1. User（用户实体）
- UserId (Guid)、UserName、Avatar、Status、LastOnlineTime、PublicKey（用于加密）

### 2. Message（消息实体 - 重构）
- MessageId、SessionId、SenderId、ReceiverId、MessageType、Content、Status、SentTime、ReadTime
- IsEncrypted、EncryptionKeyId（关联加密）

### 3. ChatSession（聊天会话）
- SessionId、SessionType（私聊/群聊）、Participants、LastMessage、UnreadCount

### 4. FileAttachment（文件附件）
- AttachmentId、MessageId、FileName、FileType、FileSize、FileUri、ThumbnailUri

### 5. MessageFriends（好友关系）
- FriendshipId、UserId、FriendId、Status、CreatedTime

---

## 二、群聊功能实体

### Group（群聊）
- GroupId、GroupName、GroupAvatar、OwnerId、MaxMembers、CreatedTime

### GroupMember（群成员）
- MemberId、GroupId、UserId、Role（群主/管理员/成员）、JoinTime、MuteEndTime

### GroupRole（群角色权限）
- RoleId、GroupId、RoleName、Permissions（发言/禁言/踢人等）

---

## 三、消息撤回机制实体

### MessageRecall（消息撤回记录）
- RecallId、MessageId、RecalledBy、RecallTime、RecallReason
- OriginalContent（可选保留）、IsWithinTimeLimit（是否在时限内）

### RecallConfig（撤回配置）
- MaxRecallTimeMinutes（默认2分钟）

---

## 四、消息转发功能实体

### MessageForward（消息转发记录）
- ForwardId、OriginalMessageId、ForwardedBy、TargetSessionId、ForwardTime
- ForwardType（直接转发/引用转发）、ForwardPath（转发链路）

---

## 五、消息已读回执实体

### MessageReadReceipt（已读回执）
- ReceiptId、MessageId、ReaderId、ReadTime、Status（已送达/已读）

### GroupReadReceipt（群聊已读回执）
- ReceiptId、MessageId、GroupId、ReadUserIds、UnreadUserIds

---

## 六、离线消息存储实体

### OfflineMessage（离线消息）
- OfflineId、UserId、MessageId、StoredTime、ExpireTime（3天）
- IsDelivered、DeliveredTime

### OfflineMessageConfig（离线配置）
- MaxStorageDays = 3、MaxStorageSizeMB = 1024

### UserOfflineStorage（用户离线存储统计）
- UserId、TotalSize、MessageCount、LastCleanupTime

---

## 七、消息加密功能实体

### MessageEncryption（消息加密）
- EncryptionId、MessageId、Algorithm（AES-256-GCM/RSA）
- EncryptedData、IV（初始化向量）、Tag（认证标签）

### EncryptionKey（加密密钥）
- KeyId、UserId、PublicKey、PrivateKey（加密存储）、CreatedTime、ExpireTime

### SessionKey（会话密钥）
- KeyId、SessionId、EncryptedKey、KeyHolders（密钥持有者）

---

## 八、领域事件

- MessageSentEvent、MessageReceivedEvent、MessageReadEvent
- MessageRecalledEvent、MessageForwardedEvent
- FileUploadedEvent、FileDownloadedEvent
- UserOnlineEvent、UserOfflineEvent
- GroupCreatedEvent、GroupMemberJoinedEvent、GroupMemberLeftEvent
- FriendshipCreatedEvent、FriendshipAcceptedEvent

---

## 九、list.md 任务清单

创建任务追踪文件，记录所有设计任务及进度状态。

---

## 文件结构

```
Message.Domain/
├── Entities/
│   ├── User.cs
│   ├── Message.cs
│   ├── ChatSession.cs
│   ├── FileAttachment.cs
│   ├── MessageFriends.cs
│   ├── Group/
│   │   ├── Group.cs
│   │   ├── GroupMember.cs
│   │   └── GroupRole.cs
│   ├── Recall/
│   │   ├── MessageRecall.cs
│   │   └── RecallConfig.cs
│   ├── Forward/
│   │   └── MessageForward.cs
│   ├── Receipt/
│   │   ├── MessageReadReceipt.cs
│   │   └── GroupReadReceipt.cs
│   ├── Offline/
│   │   ├── OfflineMessage.cs
│   │   ├── OfflineMessageConfig.cs
│   │   └── UserOfflineStorage.cs
│   ├── Encryption/
│   │   ├── MessageEncryption.cs
│   │   ├── EncryptionKey.cs
│   │   └── SessionKey.cs
│   └── Enums/
│       ├── MessageType.cs
│       ├── UserStatus.cs
│       ├── SessionType.cs
│       ├── MessageStatus.cs
│       ├── FriendshipStatus.cs
│       ├── GroupMemberRole.cs
│       └── EncryptionAlgorithm.cs
├── Events/
│   └── [领域事件文件]
├── ValueObjects/
│   └── [值对象]
├── IRepository/
│   └── [仓储接口]
├── SeedWork/
│   └── [基础类]
└── list.md
```
