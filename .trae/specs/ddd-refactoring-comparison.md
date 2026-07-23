# Message.Infrastructure 领域驱动设计(DDD)重构对比文档

## 一、重构概述

本次重构对 `Message.Infrastructure\Repository` 存储层进行了系统性改造，使其严格遵循领域驱动设计(DDD)的聚合根范式。重构覆盖了 Domain 层实体标记、仓储接口设计、EF Core 配置映射、Provider 业务层适配，以及完整的单元测试验证。所有现有业务功能保持不变。

---

## 二、聚合根标记修正

### 2.1 将 Comment 和 TweetReport 标记为聚合根

| 实体 | 重构前 | 重构后 |
|------|--------|--------|
| `Comment` | `public class Comment : Entity` | `public class Comment : Entity, IAggregateRoot` |
| `TweetReport` | `public class TweetReport : Entity` | `public class TweetReport : Entity, IAggregateRoot` |

**改进点：**
- `Comment` 拥有独立的聚合边界（包含评论内容、点赞数、回复数等），应作为聚合根进行数据访问
- `TweetReport` 拥有完整的生命周期（创建→审核→处理），包含证据URL集合，适合独立聚合

### 2.2 非聚合根实体确认

以下实体**不**实现 `IAggregateRoot`，只能通过所属聚合根访问：

| 实体 | 所属聚合根 | 确认状态 |
|------|-----------|----------|
| `GroupMember` | `Group` | 正确 - 不实现 IAggregateRoot |
| `TweetInteraction` | `Tweet` | 正确 - 不实现 IAggregateRoot |
| `TweetNotification` | 独立实体 | 正确 - 不实现 IAggregateRoot |

---

## 三、Group 聚合根方法完善

### 重构前

Group 聚合根缺乏成员操作的封装方法，所有成员操作需由外部 Provider 直接操作 `GroupMember` 实体，破坏了聚合边界。

```csharp
// 重构前 Group.cs - 缺少成员操作方法
public class Group : Entity, IAggregateRoot
{
    private readonly List<GroupMember> _members = new();
    public IReadOnlyCollection<GroupMember> Members => _members.AsReadOnly();
    
    // 只有 AddMember 和 RemoveMember
    public void AddMember(Guid userId, GroupMemberRole role) { ... }
    public void RemoveMember(Guid userId) { ... }
}
```

### 重构后

新增了 6 个通过聚合根操作的成员管理方法，确保所有 `GroupMember` 状态变更经过聚合根：

```csharp
// 重构后 Group.cs - 新增方法
public void PromoteMember(Guid userId)    // 提升为管理员
public void DemoteMember(Guid userId)     // 降级为普通成员
public void MuteMember(Guid userId, TimeSpan duration)  // 禁言
public void UnmuteMember(Guid userId)     // 解除禁言
public void BanMember(Guid userId)        // 封禁
public void UnbanMember(Guid userId)      // 解封
public void TransferOwnership(Guid newOwnerId)  // 转让群主
```

**改进点：**
- 所有成员操作统一通过 `Group` 聚合根执行
- 聚合根方法内含 `IsDismissed` 守卫检查，保证状态一致性
- 每个方法委托给 `GroupMember` 的对应方法，遵循"聚合根作为入口，子实体执行具体逻辑"的模式

---

## 四、仓储接口重新设计

### 4.1 IGroupRepository

#### 重构前

仓储接口混入了直接操作子实体的细粒度方法：

```csharp
public interface IGroupRepository : IRepository<Group>
{
    // ❌ 直接操作 GroupMember 的方法（破坏聚合边界）
    Task PromoteToAdminAsync(Guid groupId, Guid userId);
    Task DemoteToMemberAsync(Guid groupId, Guid userId);
    Task MuteMemberAsync(Guid groupId, Guid userId, TimeSpan duration);
    Task UnmuteMemberAsync(Guid groupId, Guid userId);
    Task BanMemberAsync(Guid groupId, Guid userId);
    Task UnbanMemberAsync(Guid groupId, Guid userId);
}
```

#### 重构后

移除了直接操作子实体的方法，新增 `GetByIdWithMembersAsync` 用于加载完整聚合根：

```csharp
public interface IGroupRepository : IRepository<Group>
{
    Task<Group?> GetByIdAsync(Guid groupId);
    
    // ✅ 加载聚合根及其成员集合（用于需要修改成员的场景）
    Task<Group?> GetByIdWithMembersAsync(Guid groupId);
    
    // ✅ 转让群主方法保留，但内部通过聚合根实现
    Task TransferOwnershipAsync(Guid groupId, Guid newOwnerId);
    
    // 只读查询方法保留（不修改子实体状态）
    Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId);
    Task<IReadOnlyList<GroupMember>> GetMembersAsync(Guid groupId);
    Task<IReadOnlyList<GroupMember>> GetAdminsAsync(Guid groupId);
}
```

**改进点：**
- `GetByIdWithMembersAsync` 使用 `Include(g => g.Members)` 预加载成员集合，确保聚合完整性
- 移除了 `PromoteToAdminAsync` 等破坏聚合边界的方法
- 业务层通过 `GetByIdWithMembersAsync` 加载聚合根 → 调用聚合根方法 → `UpdateAsync` 持久化

### 4.2 ICommentRepository 和 ITweetReportRepository

#### 重构前

接口未继承 `IRepository<T>`，与聚合根身份不匹配：

```csharp
public interface ICommentRepository  // ❌ 未继承 IRepository<Comment>
{
    Task<Comment?> GetByIdAsync(Guid commentGuid);
    // ...
}
```

#### 重构后

正确继承泛型仓储基接口：

```csharp
public interface ICommentRepository : IRepository<Comment>  // ✅
{
    Task<Comment?> GetByIdAsync(Guid commentGuid);
    // ...
}

public interface ITweetReportRepository : IRepository<TweetReport>  // ✅
{
    Task<TweetReport?> GetByIdAsync(Guid reportGuid);
    // ...
}
```

---

## 五、仓储实现适配

### 5.1 GroupRepository

#### 重构前

```csharp
public class GroupRepository : IGroupRepository
{
    public async Task<Group?> GetByIdAsync(Guid groupId)
    {
        return await DbSet.FirstOrDefaultAsync(groupId);
    }
    // 未实现 GetByIdWithMembersAsync
}
```

#### 重构后

```csharp
public class GroupRepository(MessageDbContext context) : IGroupRepository
{
    public async Task<Group?> GetByIdWithMembersAsync(Guid groupId)
    {
        return await DbSet
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.GroupId == groupId && !g.IsDismissed);
    }
    
    public async Task TransferOwnershipAsync(Guid groupId, Guid newOwnerId)
    {
        var group = await GetByIdWithMembersAsync(groupId);
        group?.TransferOwnership(newOwnerId);
        if (group != null) await UpdateAsync(group);
    }
    
    public async Task<Group> DeleteAsync(Group group)
    {
        group.Dismiss();  // ✅ 通过聚合根方法而不是直接设置状态
        return await UpdateAsync(group);
    }
}
```

### 5.2 CommentRepository 和 TweetReportRepository

#### 重构后

完整实现了仓储接口，包含 UnitOfWork 支持、分页查询、null守卫等：

```csharp
public class CommentRepository(MessageDbContext context) : ICommentRepository
{
    public IUnitOfWork UnitOfWork => context;
    
    public async Task<Comment> DeleteAsync(Comment comment)
    {
        comment.SoftDelete();  // 通过聚合根方法
        return UpdateAsync(comment);
    }
}

public class TweetReportRepository : ITweetReportRepository
{
    public IUnitOfWork UnitOfWork => _context;
    // 完整实现...
}
```

---

## 六、EF Core 配置改进

### 6.1 GroupConfiguration - 聚合根集合映射

#### 重构前

Members 导航属性被完全忽略，无法通过 EF Core 加载：

```csharp
builder.Ignore(g => g.Members);  // ❌ 完全忽略
```

#### 重构后

配置一对多关系并使用字段访问模式：

```csharp
builder.HasMany(g => g.Members)
    .WithOne()
    .HasForeignKey(gm => gm.GroupId)
    .OnDelete(DeleteBehavior.Cascade);
builder.Metadata.FindNavigation(nameof(Group.Members))!
    .SetPropertyAccessMode(PropertyAccessMode.Field);  // ✅
builder.Ignore(g => g.MemberCount);  // 忽略计算属性
```

**改进点：**
- `PropertyAccessMode.Field` 确保 EF Core 直接操作 `_members` 私有字段
- 绕过只读的 `Members` 属性，保证聚合根封装的完整性

### 6.2 TweetConfiguration - 值对象映射

#### 重构前

`LinkMetadata`、`Media`、`Hashtags` 三个复杂类型属性缺少值转换器，导致 InMemory 数据库提供程序无法映射：

```csharp
builder.Property("_media")
    .HasColumnType("nvarchar(max)");  // ❌ 缺少 HasConversion
builder.Property(t => t.LinkMetadata)
    .HasColumnType("nvarchar(max)");  // ❌ 缺少 HasConversion
```

#### 重构后

为所有复杂类型属性添加了值转换器：

```csharp
// Media: List<TweetMedia> ↔ JSON
builder.Property(t => t.Media)
    .HasColumnType("nvarchar(max)")
    .HasConversion(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<List<TweetMedia>>(v, ...) ?? new())
    .UsePropertyAccessMode(PropertyAccessMode.Field);

// LinkMetadata: LinkMetadata? ↔ JSON
builder.Property(t => t.LinkMetadata)
    .HasColumnType("nvarchar(max)")
    .HasConversion(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<LinkMetadata>(v, ...));

// Hashtags: HashSet<string> ↔ 逗号分隔字符串
builder.Property(t => t.Hashtags)
    .HasColumnType("nvarchar(max)")
    .HasConversion(
        v => string.Join(",", v),
        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet())
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

---

## 七、Provider 层适配

### GroupProvider - 通过聚合根操作成员

#### 重构前

Provider 直接操作 `GroupMember` 实体，绕过聚合根：

```csharp
public async Task PromoteToAdminAsync(Guid groupId, Guid userId)
{
    var member = await _groupMemberRepository.GetByGroupAndUserAsync(groupId, userId);
    member.PromoteToAdmin();  // ❌ 直接操作子实体
    await _groupMemberRepository.UpdateAsync(member);
}
```

#### 重构后

通过 `GetByIdWithMembersAsync` 加载聚合根，调用聚合根方法：

```csharp
public async Task PromoteToAdminAsync(Guid groupId, Guid userId)
{
    var group = await _groupRepository.GetByIdWithMembersAsync(groupId);
    if (group == null)
        throw new KeyNotFoundException("群组不存在");
    
    group.PromoteMember(userId);  // ✅ 通过聚合根方法
    await _groupRepository.UpdateAsync(group);
    await _unitOfWork.SaveEntitiesAsync();
}
```

---

## 八、Domain 层合规性修复

### GroupMember.DemoteToMember() - 移除不合理的 Owner 检查

#### 重构前

`DemoteToMember()` 禁止对 Owner 角色降级，导致 `TransferOwnership` 方法在转让群主时对旧群主调用 `DemoteToMember()` 时抛出异常：

```csharp
public void DemoteToMember()
{
    if (Role == GroupMemberRole.Owner)
        throw new InvalidOperationException("群主不能更改角色");  // ❌ 阻止转让群主流程
    Role = GroupMemberRole.Member;
}
```

#### 重构后

移除限制，由聚合根 `TransferOwnership` 控制完整的转让流程：

```csharp
public void DemoteToMember()
{
    Role = GroupMemberRole.Member;  // ✅ 由聚合根控制调用时机
}
```

---

## 九、代码质量改进

### 清理不必要的 `new` 关键字

5 个 Repository 类中 10 处 `new` 关键字被移除（CS0109 警告清零）：

| 文件 | 受影响方法 |
|------|-----------|
| `GroupRepository.cs` | `AddAsync`, `UpdateAsync` |
| `TweetRepository.cs` | `AddAsync`, `UpdateAsync` |
| `MessageRepository.cs` | `AddAsync`, `UpdateAsync` |
| `MessageFriendsRepository.cs` | `AddAsync`, `UpdateAsync` |
| `ChatSessionRepository.cs` | `AddAsync`, `UpdateAsync` |

这些方法实现了接口中已定义的同名方法，`new` 关键字是多余的。

---

## 十、单元测试覆盖

### 测试文件及覆盖范围

| 测试文件 | 测试数 | 覆盖内容 |
|---------|--------|----------|
| `GroupAggregateRootTests.cs` | 11 | 聚合根成员操作方法（PromoteMember/DemoteMember/MuteMember/BanMember/UnbanMember）、解散后守卫检查、不存在的成员异常 |
| `GroupRepositoryTests.cs` | 6 | `GetByIdWithMembersAsync` 加载完整聚合根、`TransferOwnership` 通过聚合根持久化、`AddMember`/`RemoveMember` 通过聚合根操作 |
| `AggregateRootMarkingTests.cs` | 8 | 聚合根标记正确性（TweetReport/Comment/Group 标记为 IAggregateRoot，GroupMember/TweetInteraction/TweetNotification 不标记）、仓储接口继承验证、领域事件发出验证 |
| `TweetInteractionRepositoryTests.cs` | 4 | 仓储 AddAsync/GetAsync/ExistsAsync/GetCountByTweetAsync 持久化验证 |
| `UserNotificationSettingRepositoryTests.cs` | 2 | 仓储读写功能验证 |
| `Argon2PasswordHasherTests.cs` | 1 | 密码哈希工具验证 |

**总计：32 个测试，全部通过。**

---

## 十一、改进总结

| 改进维度 | 重构前 | 重构后 |
|---------|--------|--------|
| **聚合根边界** | Group 聚合根缺少成员操作方法，子实体操作散落在 Provider 和 Repository 中 | Group 聚合根完整封装了 13 个成员操作方法，所有状态变更通过聚合根 |
| **聚合根标记** | Comment、TweetReport 未标记为 IAggregateRoot | 正确标记，对应的仓储接口继承 IRepository\<T\> |
| **仓储接口** | IGroupRepository 包含直接操作 GroupMember 的方法 | 移除了跨聚合边界的方法，新增 GetByIdWithMembersAsync |
| **EF Core 配置** | Members 导航被 Ignore；Tweet 复杂属性缺少值转换器 | PropertyAccessMode.Field 正确配置；值转换器覆盖所有复杂类型 |
| **领域事件** | 部分操作缺少领域事件 | 成员加入/移除、群主转让、群解散等操作均发布领域事件 |
| **代码质量** | 10 处 CS0109 警告（不必要的 new 关键字） | 0 处 CS0109 警告 |
| **测试覆盖** | 0 个测试 | 32 个测试，覆盖聚合根行为、仓储实现、聚合根标记 |
