# Permission 实体在微服务中的角色与实现方案（修订版）

> 基于 NotBlog 项目 DDD + CQRS + EventBus + Aspire 架构的最终设计  
> 本文档仅提供思路，不涉及任何代码修改

---

## 一、5 项关键架构决策

| # | 决策点   | 最终选择                          | 影响                        |
|---|-------|-------------------------------|---------------------------|
| 1 | 鉴权位置  | **网关统一鉴权**（YARP 反向代理）         | 集中管理，无效请求在网关层即被拦截         |
| 2 | 权限粒度  | **功能级 + 数据行级**                | 既控制谁能调用 API，也控制谁能访问特定数据   |
| 3 | 过滤器归属 | **独立项目 `NotBlog.Permission`** | 见下方专节说明                   |
| 4 | 管理员特权 | **不跳过任何校验**                   | 防止权限泄露，管理员也需走完整鉴权链路       |
| 5 | 实时性要求 | **实时校验，不缓存**                  | 每次请求都通过 gRPC 实时查询权限，零延迟感知 |

---

## 二、关于 PermissionFilter 为什么不应放在 CacheMemory 中

### 结论：创建独立项目 `NotBlog.Permission`

### 理由

| 维度    | CacheMemory 的职责        | Permission 的职责   | 是否重叠？  |
|-------|------------------------|------------------|--------|
| 核心关注点 | Redis 缓存、连接管理、重试策略     | 权限判断、鉴权规则、授权决策   | ❌ 不重叠  |
| 领域归属  | 基础设施（Infrastructure）   | 安全/授权（Security）  | ❌ 不同   |
| 依赖方向  | 被 NotEmail、Identity 引用 | 被所有 Web.API 项目引用 | 可以独立   |
| 变更原因  | Redis 版本升级、重试策略调整      | 权限模型变更、新增鉴权规则    | ❌ 独立变更 |

**体量分析**：`PermissionFilter` + `RequiredPermissionAttribute` + `IPermissionChecker` 接口 ≈ 100~200
行代码。虽体量不大，但职责清晰独立，具备独立项目的合理性：

```
NotBlog.Permission/
├── Core/
│   ├── IPermissionChecker.cs          # 权限检查接口
│   ├── RequiredPermissionAttribute.cs # [RequiredPermission("code")] 特性
│   └── PermissionAuthorizationData.cs # 权限数据 DTO
├── Infrastructure/
│   ├── GrpcPermissionChecker.cs       # 通过 gRPC 实时查询权限
│   └── PermissionMiddleware.cs        # ASP.NET Core 中间件
└── NotBlog.Permission.csproj
```

> **类比**：`JWToken` 项目也只做 Token 签发/验证，体量不大但独立存在。权限鉴权同样具备独立价值。

---

## 三、修订后架构总览

```
┌──────────────────────────────────────────────────────┐
│                    YARP API Gateway                   │
│  ┌─────────────────────────────────────────────────┐ │
│  │  JWT 验证 → 提取 user_id → gRPC 查权限 → 匹配   │ │
│  │  命中 → 路由到目标服务    未命中 → 直接返回 403   │ │
│  └─────────────────────────────────────────────────┘ │
└──────────┬────────────────────┬──────────────────────┘
           │                    │
     ┌─────▼─────┐        ┌─────▼─────┐
     │Identity   │        │FileDev/   │
     │gRPC       │◀───────│Markdown/  │  ← 网关已鉴权，直接处理
     │权限查询   │  gRPC  │Message    │
     └───────────┘        └───────────┘
```

### 请求流程

```
客户端请求 → YARP Gateway
    │
    ├── 1. JWT 中间件：验证签名、过期 → 提取 user_id + role_codes
    │
    ├── 2. 权限中间件：提取当前请求的 URL/Method
    │      ├ 映射为 PermissionCode（如 GET /api/articles → "api:article:read"）
    │      ├ gRPC → Identity.CheckPermission(user_id, permission_code)
    │      │           Identity 查 DB：User → Roles → Permissions 求并集
    │      │           ← 返回 true/false
    │      └ false → 直接返回 403（不转发到下游服务）
    │
    └── 3. 鉴权通过 → YARP 转发到 FileDev/Markdown/... 服务
```

---

## 四、功能级权限（API/菜单）— 网关统一鉴权

### 4.1 JWT Claims 设计（不变）

```json
{
  "sub": "user-guid-xxx",
  "user_id": "01932abc-...",
  "user_name": "zhangsan",
  "role": [
    "ADMIN",
    "EDITOR"
  ],
  "iat": 1234567890,
  "exp": 1234567890
}
```

### 4.2 权限映射表（Gateway 维护）

YARP Gateway 维护 URL → PermissionCode 的路由映射：

```csharp
// 网关配置示例
var permissionRoutes = new Dictionary<string, string>
{
    ["GET    /api/articles"]         = "api:article:read",
    ["POST   /api/articles"]         = "api:article:create",
    ["PUT    /api/articles/{id}"]    = "api:article:update",
    ["DELETE /api/articles/{id}"]    = "api:article:delete",
    ["GET    /api/admin/dashboard"]  = "menu:dashboard",
    ["POST   /api/admin/users"]      = "api:user:create",
};
```

> ⚠️ 网关需要知道所有服务的所有路由对应的权限码。这要求制定统一的 **PermissionCode 命名规范**。

### 4.3 Identity gRPC 实时查询接口

```protobuf
service PermissionService {
  // 检查用户是否拥有某个权限（实时查 DB）
  rpc CheckPermission (CheckPermissionRequest) returns (CheckPermissionResponse);

  // 获取用户全部权限列表（用于管理后台展示）
  rpc GetUserPermissions (UserPermissionsRequest) returns (UserPermissionsResponse);
}

message CheckPermissionRequest {
  string user_id = 1;
  string permission_code = 2;
}

message CheckPermissionResponse {
  bool has_permission = 1;
}
```

**查询逻辑（每次实时执行）：**

```
user_id → User.UserRoleGuid[] → Roles[]
    ├── Permissions (直连权限)
    └── RoleGroups[] → Permissions (组权限)
                    ↓
              合并去重 → 判断是否包含目标 permission_code
```

> ⚠️ 由于是实时查询，每次鉴权请求都会访问 DB。对 Identity 数据库的读压力较高，建议：
> - Permissions 表数据量小（通常 < 200 条），查询走索引很快
> - 网关与 Identity gRPC 通过 localhost 通信，延迟 < 1ms
> - 如果未来 QPS 达到万级，可考虑 Identity 侧加内存缓存（1 秒 TTL）

### 4.4 管理员不跳过校验

```csharp
// ❌ 错误的做法
if (user.Roles.Contains("ADMIN")) {
    return true; // 直接放行 —— 权限泄露风险！
}

// ✅ 正确的做法
// ADMIN 角色同样要走完整的 Permission 匹配流程
// ADMIN 只是在 DB 中被授予了更多 PermissionCode
var hasPermission = await identityGrpc.CheckPermissionAsync(user_id, required_code);
return hasPermission;
```

管理员之所以"什么都能做"，是因为在权限分配时被授予了全部 PermissionCode，而非代码中硬编码跳过。

---

## 五、数据行级权限 — Domain 层 Specification 模式

### 5.1 问题场景

功能级权限回答"能不能调用 API"，行级权限回答"能访问哪些数据"。

示例：

- 用户 A（作者角色）→ 能调 `GET /api/articles` → 但只能看到**自己的**文章
- 用户 B（管理员角色）→ 能调 `GET /api/articles` → 能看到**所有**文章

### 5.2 行级权限判定逻辑

行级权限无法在网关完成（网关不知道业务数据），必须在各服务内部处理。

**方案：通过 JWT Claims 传递数据范围**

```
JWT Claims 中新增：
{
  "data_scope": {
    "type": "OWN",          // OWN(本人) | ALL(全部) | DEPT(部门)
    "values": []            // 额外的数据 ID 白名单
  }
}
```

`data_scope` 由 Identity 服务在登录时根据用户角色计算：

| 角色   | data_scope.type                  | 含义          |
|------|----------------------------------|-------------|
| 普通用户 | `OWN`                            | 只能访问自己的数据   |
| 编辑   | `DEPARTMENT` + values: [dept_id] | 只能访问自己部门的数据 |
| 管理员  | `ALL`                            | 可以访问所有数据    |

### 5.3 在各服务中应用行级权限

以 Markdown 服务查询文章为例：

```csharp
// Application 层
public async Task<IEnumerable<ArticleDto>> GetArticlesAsync(
    Guid userId, DataScope dataScope, CancellationToken ct)
{
    var query = _dbContext.Articles.AsQueryable();

    // 根据 data_scope 自动追加过滤条件
    query = dataScope.Type switch
    {
        "OWN"        => query.Where(a => a.AuthorId == userId),
        "DEPARTMENT" => query.Where(a => dataScope.Values.Contains(a.DepartmentId)),
        "ALL"        => query, // 不过滤
        _            => throw new InvalidOperationException("Unknown data scope")
    };

    return await query.ToListAsync(ct);
}
```

### 5.4 更新/删除的行级权限

```csharp
// Domain 层：聚合根自己校验所有权
public class Article : Entity, IAggregateRoot
{
    public Guid AuthorId { get; private set; }

    public void Update(string title, Guid currentUserId, IUserContext userContext)
    {
        // 不是自己创建的 + 没有 ALL 权限 → 拒绝
        if (AuthorId != currentUserId && !userContext.DataScope.IsAll)
            throw new UnauthorizedAccessException("无权修改此文章");

        Title = title;
    }
}
```

---

## 六、PermissionCode 命名规范

为保证网关映射一致性，制定统一规范：

```
格式：{资源类型}:{资源}:{操作}

示例：
  api:article:read         # API 权限 - 文章读取
  api:article:create       # API 权限 - 文章创建
  api:article:delete       # API 权限 - 文章删除
  api:user:manage          # API 权限 - 用户管理
  menu:dashboard           # 菜单权限 - 仪表盘
  menu:content             # 菜单权限 - 内容管理
  file:upload              # 文件权限 - 上传
  file:download            # 文件权限 - 下载
```

---

## 七、项目结构终态

```
NotBlog.sln
├── NotBlog.Permission/          ← 🆕 独立权限项目
│   ├── Core/
│   │   ├── IPermissionChecker.cs
│   │   ├── RequiredPermissionAttribute.cs
│   │   └── DataScope.cs
│   └── Infrastructure/
│       ├── GrpcPermissionChecker.cs
│       └── PermissionMiddleware.cs
│
├── NotBlog.Gateway/              ← 🆕 YARP API Gateway
│   ├── Program.cs
│   ├── PermissionRouteMapper.cs  # URL → PermissionCode 映射
│   └── appsettings.json
│
├── IdentityGrpc/                 ← ✏️ 已有，新增 CheckPermission RPC
├── CacheMemory/                   (不受影响)
├── Identity.Domain/              ← ✏️ 已有，新增 DataScope 计算逻辑
├── FileDev.Domain/               ← ✏️ 新增行级权限过滤
└── ... 其他服务同样新增行级权限
```

---

## 八、实施路径

| 阶段          | 内容                                         | 涉及项目                                  | 复杂度  |
|-------------|--------------------------------------------|---------------------------------------|------|
| **Phase 1** | 制定 PermissionCode 命名规范                     | 文档先行                                  | ⭐    |
| **Phase 2** | 创建 `NotBlog.Permission` 项目（接口 + Attribute） | 新项目                                   | ⭐⭐   |
| **Phase 3** | IdentityGrpc 新增 `CheckPermission` RPC      | IdentityGrpc, Identity.Infrastructure | ⭐⭐⭐  |
| **Phase 4** | 创建 `NotBlog.Gateway`（YARP + JWT + 权限中间件）   | 新项目                                   | ⭐⭐⭐⭐ |
| **Phase 5** | 登录时计算并签发 `data_scope` 到 JWT                | Identity.Web.API, JWToken             | ⭐⭐   |
| **Phase 6** | 逐步在各服务 Domain 层添加行级权限过滤                    | FileDev/Markdown/Message              | ⭐⭐⭐  |
| **Phase 7** | 管理后台维护 Permissions、Roles、RoleGroups        | Identity.Web.API                      | ⭐⭐⭐  |

---

## 九、性能考量

### 实时校验的代价

由于每请求都 gRPC 调 Identity 查 DB，需要关注：

| 场景        | 估算 QPS       | 应对策略                             |
|-----------|--------------|----------------------------------|
| 博客站点（低流量） | < 100 QPS    | 直接 gRPC + DB 查询，无需优化             |
| 中等流量      | 100-1000 QPS | Identity 侧加 1s 内存缓存              |
| 高流量       | > 1000 QPS   | Gateway 侧本地缓存 + Identity 侧发布失效事件 |

> 本项目为小型博客系统，建议先按"直接实时查询"实现，实际遇到瓶颈再优化。

### Gateway 不成为瓶颈

- YARP 本身高性能（基于 Kestrel + ASP.NET Core 管道）
- gRPC 调用 Identity 走本地 localhost，延迟可忽略
- Permission 表数据量小，DB 查询走主键/索引，单次 < 5ms
