# Message 语音 / 视频通话模块设计文档（CallHub）

> 实现时间：2026-08-14
> 技术路线：**SignalR 信令 + WebRTC 媒体**（服务端仅转发信令，媒体为 P2P 直连，Mesh 全网状拓扑）

## 一、概述

在 Message 模块中新增语音通话与视频通话能力，基于项目现有 SignalR 框架：

- **端点**：`/CallHub`（独立 Hub，`[Authorize]` JWT 强制认证，与 MessageHub 同源）
- **范围**：1 对 1（私聊会话）与群组多人通话（群聊/频道会话），成员 = 会话参与者
- **发起入口**：会话 `sessionId`（服务端强校验参与者身份，防越权）
- **持久化**：不落库，仅实时信令；通话状态存 Redis（跨实例一致 + TTL 兜底）

## 二、架构

```
┌──────────┐   SignalR WebSocket    ┌─────────────────────────────┐
│ 浏览器    │ ─────────────────────▶ │ Message.Web.API /CallHub     │
│ (WebRTC   │ ◀───────────────────── │  CallHub (信令收发)           │
│  Mesh 对等 │   IncomingCall/信令...  │    │                        │
│  连接)     │                        │  CallSessionStore (Singleton)│
└──────────┘                        │    │ Redis 状态 + 连接推送     │
                                    └────┼────────────────────────┘
                                         ▼
                                  Redis: message:call:* / message:user:*
```

| 组件 | 生命周期 | 职责 |
|------|---------|------|
| `CallHub` | Transient（SignalR 框架） | Hub 方法：StartCall/CancelCall/AcceptCall/RejectCall/JoinCall/HangUp/SendSignal/GetCall |
| `CallSessionStore` | Singleton | 通话状态机 + Redis 存取 + 经 `IHubContext` 推送（连接查询走作用域工厂） |
| `ICallClient` | — | 客户端事件接口（服务端 → 客户端） |
| `Dto/Call/*` | — | 通话 DTO：CallInfoDto / CallSignalDto / CallStartResult 及枚举 |

## 三、协议

### 3.1 客户端 → 服务端（Hub 方法）

| 方法 | 参数 | 说明 |
|------|------|------|
| `StartCall` | `(Guid sessionId, CallType type)` | 发起呼叫，返回 `CallStartResult`（含 busy/offline 成员）；呼叫方立即标记忙线 |
| `CancelCall` | `(Guid callId)` | 呼叫方响铃阶段取消 |
| `AcceptCall` | `(Guid callId)` | 接听：首名接通 → 通话建立；后续调用等价 JoinCall |
| `RejectCall` | `(Guid callId)` | 拒绝：1 对 1 结束通话；群组仅通知呼叫方 |
| `JoinCall` | `(Guid callId)` | 加入通话（群组后续成员 / 断线重连恢复） |
| `HangUp` | `(Guid callId)` | 挂断：1 对 1 结束；群组广播 MemberLeft，最后一人离开时结束 |
| `SendSignal` | `(CallSignalDto)` | 转发 WebRTC 信令（offer/answer/ice）；`FromUserId` 服务端权威填充 |
| `GetCall` | `(Guid callId)` | 查询通话状态（重连恢复用） |

### 3.2 服务端 → 客户端（ICallClient 事件）

| 事件 | 参数 | 说明 |
|------|------|------|
| `IncomingCall` | `CallInfoDto` | 被叫收到来电（响铃） |
| `CallStarted` | `CallInfoDto` | 通话建立（含 JoinedMembers 快照） |
| `CallEnded` | `(CallInfoDto, CallEndReason)` | 通话结束（取消/拒绝/超时/全员离开/对方挂断/错误） |
| `MemberJoined` | `(CallInfoDto, Guid memberId)` | 成员接通，既有成员据此与其建立对等连接 |
| `MemberLeft` | `(CallInfoDto, Guid memberId)` | 成员离开（群组） |
| `MemberRejected` | `(CallInfoDto, Guid memberId)` | 成员拒绝（通知呼叫方） |
| `Signal` | `CallSignalDto` | WebRTC 信令定向/广播转发 |

### 3.3 信令约定（Mesh 拓扑）

- 通话建立或成员加入后，**由新加入成员向通话内每个既有成员发送 `offer`**（规则统一）；
- 被邀方回 `answer`，双方随时互发 `ice`；
- 定向信令（`ToUserId` 非空）转发给目标成员全部在线连接；广播信令（`ToUserId` 为空）转发给通话内其他已接通成员；
- 服务端不解析 SDP/ICE 内容，仅做合法性校验（通话 Active + 发送方已接通）。

## 四、呼叫流程

### 4.1 1 对 1 语音/视频呼叫

```
呼叫方 A                     CallSessionStore                 被叫 B
   │ StartCall(sessionId,type)  │                                │
   │───────────────────────────▶│ 校验会话参与者/忙线 → 创建 Ringing│
   │                            │────────────────────────────────▶│ IncomingCall
   │                            │◀────────────────────────────────│ AcceptCall
   │◀──────── CallStarted ──────│ 置 Active，A、B 标记 Joined      │
   │──────── offer ────────────▶│──────── 转发 offer ────────────▶│
   │◀──────── answer ───────────│◀──────── 转发 answer ───────────│
   │◀───────── ice ─────────────│───────── 转发 ice ─────────────▶│ (双向)
   │──────── HangUp ───────────▶│ EndCall(RemoteHangup)           │
   │◀────── CallEnded ──────────│────────────────────────────────▶│ CallEnded
```

### 4.2 群组多人通话

```
A StartCall(群会话) → 在线成员收 IncomingCall（忙线成员标记 Busy）
B AcceptCall        → Active；A、B 收 CallStarted/MemberJoined → A↔B offer/answer/ice
C JoinCall          → 已接通成员收 MemberJoined → C 向 A、B 各发 offer（Mesh）
D HangUp            → 其余成员收 MemberLeft；最后一名成员离开 → 全员 CallEnded(AllLeft)
```

## 五、状态机与边界规则

| 状态 | 进入条件 | 离开条件 |
|------|---------|---------|
| `Ringing` | StartCall | 首名成员 Accept（→Active）；Cancel/Reject/超时（→Ended） |
| `Active` | 首名接通 | 挂断/全员离开/对方挂断（→Ended） |
| `Ended` | 终态 | —（Redis 清理） |

- **忙线**：用户在任一通话中（`message:call:user:{userId}` 存在）即忙；StartCall 返回 `BusyUsers`，忙线成员不推送 IncomingCall；
- **离线**：StartCall 返回 `OfflineUsers`（UI 提示"对方离线"），不推送 IncomingCall；
- **超时**：Ringing 超 30 秒服务端惰性终结（Timeout）——客户端操作触发检查，Redis TTL（6h）兜底防泄漏；
- **断线**：用户全部连接断开 → 自动移出通话（1 对 1 因此结束，群组广播 MemberLeft）；
- **多端**：事件推送到用户全部在线连接；任一设备 Accept/HangUp 均生效（状态按用户维度记录）。

## 六、Redis 键

| Key | 内容 | TTL |
|-----|------|-----|
| `message:call:{callId}` | CallSession JSON（状态/成员/忙线列表） | 6h |
| `message:call:user:{userId}` | 用户当前通话 callId（忙线判定/断线清理） | 6h |

## 七、网关（NotBlog_Yarp）

- `PermissionRoutes.PublicPaths` 新增 `/CallHub`、`/CommunityHub`（与 `/MessageHub` 一致：网关放行，认证由服务自身 [Authorize] 完成）；
- `ReverseProxy.Routes` 新增 `message-call-hub`、`message-community-hub` → `message-cluster`。

## 八、测试

- `Message.Tests/Services/CallSessionStoreTests.cs`：14 个状态机用例（内存 Redis + Mock Hub），覆盖呼叫创建（在线/忙线/离线）、接听、拒绝、挂断、取消、信令校验、超时；
- 运行：`dotnet test Message.Tests --filter CallSessionStoreTests`；
- `MessageHubTester/index.html` 新增通话面板：连接 CallHub → StartCall → 自动/手动 Accept → 事件日志实时展示完整信令流转（验证信令链路；真实媒体流由客户端用 `RTCPeerConnection` 建立）。

## 九、客户端集成要点（WebRTC）

1. 连接 `/CallHub`（`accessTokenFactory` 返回 JWT，或 URL 拼 `?access_token=`）；
2. 收到 `IncomingCall` → 展示来电 UI → `AcceptCall` / `RejectCall`；
3. `CallStarted`/`MemberJoined` 后：向 `JoinedMembers` 中除自己外的每个成员 `SendSignal(offer)`（新成员主动发 offer）；
4. 收到定向 `Signal(offer)` → 创建 PeerConnection → 回 `answer`；收到 `ice` → `addIceCandidate`；
5. `CallEnded` → 关闭全部 PeerConnection 与本地媒体流；
6. 挂断/取消 → `HangUp`/`CancelCall`；重连后 `GetCall` + `JoinCall` 恢复。
