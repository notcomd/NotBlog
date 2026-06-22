# DomainInfrastructure 类库系统性整理计划

## 一、当前状态分析

### 1. 项目概览

`DomainInfrastructure` 是 NotBlog 解决方案中的共享基础设施类库，目标框架 `net10.0`。

**依赖项：**

* `DomainCommons`（项目引用）

* `Microsoft.EntityFrameworkCore` 10.0.9

* `Microsoft.Extensions.DependencyInjection` / `.Abstractions` 10.0.9

* `Microsoft.AspNetCore.Mvc.Core` 2.3.11（⚠️ 版本过旧，已确认暂不处理）

### 2. 文件清单与模块划分

| 文件                           | 职责                                                      | 问题等级                      |
| ---------------------------- | ------------------------------------------------------- | ------------------------- |
| `BaseDbContext.cs`           | EF Core 基础 DbContext，集成领域事件派发                           | **严重** - 未被任何项目使用，用户确认→删除 |
| `MediatorExtensions.cs`      | 领域事件分发扩展（`Func<IDomainEvent, ValueTask>` + `DbContext`） | **严重** - 签名与实际使用不匹配       |
| `EFCoreInitializerHelper.cs` | 反射扫描注册所有 DbContext                                      | **中等** - 反射查找脆弱           |
| `UnitOfWorkFilter.cs`        | ASP.NET MVC ActionFilter 自动 SaveChanges                 | **低** - 已确认暂不处理           |
| `SaverDbContextAttribute.cs` | 标记需要 UnitOfWork 保存的 DbContext                           | **低**                     |
| `ModuleInitializer.cs`       | 扫描并执行 IModuleInitializer 实现                             | **低**                     |
| `EFCoreExtension.cs`         | 软删除全局过滤器 + Query 扩展                                     | **低**                     |
| `ReflectionHelper.cs`        | 程序集反射扫描工具                                               | **低** - 部分逻辑可优化           |
| `EnumerableExtensions.cs`    | 序列忽略顺序比较                                                | **低** - 缺少泛型约束            |
| `FormattableStringHelper.cs` | URI 安全字符串构建                                             | **正常**                    |

***

## 二、已识别的问题详解

### 问题 1：`MediatorExtensions.cs` 签名错误，导致无法被项目使用

**现状：**

```csharp
// DomainInfrastructure/MediatorExtensions.cs - 当前签名
public static async Task DispatchDomainEventsAsync(
    this Func<IDomainEvent, ValueTask> dispatcher,  // 扩展目标是 Func<> 而非 INotMediator
    DbContext dbContext)
```

**实际使用（各 Infrastructure 项目中的** **`NotMediatorExtension.cs`）：**

```csharp
// 每个项目都复制了这段逻辑，扩展目标是 INotMediator
public static async Task DispatchDomainEventsAsync(
    this INotMediator mediator,  // 正确的扩展目标
    IdentityDbContext context,   // 具体 DbContext 类型
    ...)
```

**影响：**

* `MediatorExtensions.cs` 在 DomainInfrastructure 中完全无法被使用

* 导致 5 个 Infrastructure 项目（Identity、Message、Markdown、FileDev、Video）各自复制了几乎相同的 `NotMediatorExtension.cs`

* 代码重复总量约 200+ 行，维护成本高

**修复方案：**

1. 重写 `MediatorExtensions.cs`，扩展目标改为 `INotMediator`，参数改为 `DbContext`
2. DomainInfrastructure 添加 `NotMediator` 1.0.5 包引用
3. 删除 5 个 Infrastructure 项目中的重复 `NotMediatorExtension.cs`
4. 更新各 DbContext 调用从各项目的 `NotMediatorExtension` 切换到 `DomainInfrastructure.MediatorExtensions`

***

### 问题 2：`BaseDbContext.cs` — 用户确认删除

**现状：**

* 从未被任何 DbContext 子类继承

* 所有 DbContext 直接继承 `DbContext` + 自己的领域事件派发逻辑

**处理：** 直接删除 `BaseDbContext.cs`

***

### 问题 3：`EFCoreInitializerHelper.cs` 反射注册脆弱

```csharp
var method = typeof(EntityFrameworkServiceCollectionExtensions)
    .GetMethods()
    .FirstOrDefault(m =>
        m.Name == nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext)
        && m.IsGenericMethod
        && m.GetParameters().Length == 4);  // 硬编码参数数量
```

**风险：** EF Core 版本升级时 `AddDbContext` 方法签名变化会导致运行时异常。

**修复方案：** 改为匹配参数类型而非数量，增加 `BindingFlags` 限制。

***

### 问题 4：`UnitOfWorkFilter.cs` 依赖 MVC Core 版本过旧

* `Microsoft.AspNetCore.Mvc.Core` 版本 2.3.11，目标框架 net10.0

* **用户确认：暂不处理，仅记录**

***

### 问题 5：`EnumerableExtensions.cs` 缺少泛型约束

`OrderBy(x => x)` 要求 `T` 实现 `IComparable<T>`，但方法签名上无约束。

***

### 问题 6：`ReflectionHelper.cs` 部分逻辑可优化

* `AssemblyNameComparer.GetHashCode` 中 `obj.GetName().FullName.GetHashCode()` 有潜在 NRE 风险

* `IsSystemAssembly` 启发式判断不够精确（依赖 `AssemblyCompanyAttribute`）

***

### ~~问题 7：项目命名~~ — 用户确认暂不重命名

当前名称 `DomainInfrastructure` 保持不变。

***

## 三、拟定的整理方案（已根据用户决策更新）

### 阶段 A：修复 `MediatorExtensions.cs` + 清理重复代码（最高优先级）

**DomainInfrastructure 侧：**

1. 添加 NuGet 包引用：`NotMediator` 1.0.5
2. 重写 `DispatchDomainEventsAsync` 签名：

   ```csharp
   public static async Task DispatchDomainEventsAsync(
       this INotMediator mediator,
       DbContext dbContext,
       CancellationToken cancellationToken = default,
       bool parallel = false)
   ```
3. 内部使用 `IDomainEvents` 接口（来自 `DomainCommons`）获取领域事件，而非访问具体 Entity 类型。同时兼容 `Entity.DomainEventbus` 属性模式（fallback）。
4. 补充完整的中文 XML 文档注释

**5 个 Infrastructure 项目侧（同步清理）：**

| 项目                        | 删除文件                      | 修改调用处                                                    |
| ------------------------- | ------------------------- | -------------------------------------------------------- |
| `Identity.Infrastructure` | `NotMediatorExtension.cs` | `IdentityDbContext.SavaChangesAsync`、`SavaEntitiesAsync` |
| `Message.Infrastructure`  | `NotMediatorExtension.cs` | `MessageDbContext` 中的调用                                  |
| `Markdown.Infrastructure` | `NotMediatorExtension.cs` | `MarkDownDbContext` 中的调用                                 |
| `FileDev.Infrastructure`  | `NotMediatorExtension.cs` | `NotFileDbContext` 中的调用（注意：命名空间是 `ConsoleApp1`，需修正）      |
| `Video.Infrastructure`    | `NotMediatorExtension.cs` | `VideoDbContext` 中的调用                                    |

***

### 阶段 B：删除 `BaseDbContext.cs`

直接删除文件，无其他修改（该文件未被任何代码引用）。

***

### 阶段 C：修复 `EFCoreInitializerHelper.cs`

增强反射查找健壮性：

```csharp
private static MethodInfo FindAddDbContextMethod()
{
    var method = typeof(EntityFrameworkServiceCollectionExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .FirstOrDefault(m =>
            m.Name == nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext)
            && m.IsGenericMethod
            && m.GetParameters().Length >= 3
            && m.GetParameters()[0].ParameterType == typeof(IServiceCollection))
        ?? throw new InvalidOperationException("无法找到 AddDbContext 方法，请检查 EF Core 版本兼容性。");
    return method;
}
```

***

### 阶段 D：修复 `EnumerableExtensions.cs`

添加泛型约束：

```csharp
public static bool SequenceIgnoredEqual<T>(this IEnumerable<T> source, IEnumerable<T> other)
    where T : IComparable<T>
```

***

### 阶段 E：优化 `ReflectionHelper.cs`

1. `AssemblyNameComparer.GetHashCode` 增加空检查
2. `IsSystemAssembly` 增加对 Microsoft SDK 程序集的判断（检查 `AssemblyCompanyAttribute` 和 `AssemblyProductAttribute` 的组合）
3. 补充 XML 文档注释

***

### 阶段 F：完善文档注释

对所有文件补充/完善中文 XML 文档注释，包括：

* 参数说明（`<param>`）

* 返回值说明（`<returns>`）

* 异常说明（`<exception>`）

* 使用示例（`<example>`），重点在 `SaverDbContextAttribute` 和 `UnitOfWorkFilter`

***

***

## 四、文件修改汇总

| 文件                                     | 修改内容                                      | 类型              |
| -------------------------------------- | ----------------------------------------- | --------------- |
| **DomainInfrastructure**               | <br />                                    | <br />          |
| `MediatorExtensions.cs`                | 重写：扩展目标改为 `INotMediator`，参数改为 `DbContext` | **Significant** |
| `BaseDbContext.cs`                     | **删除**                                    | **Significant** |
| `EFCoreInitializerHelper.cs`           | 增强反射查找健壮性，补充文档                            | Minor           |
| `EnumerableExtensions.cs`              | 添加 `IComparable<T>` 约束                    | Minor           |
| `ReflectionHelper.cs`                  | 修复 NRE 风险，优化判断逻辑                          | Minor           |
| `DomainInfrastructure.csproj`          | 添加 `NotMediator` 1.0.5 包引用                | Minor           |
| **Identity.Infrastructure**            | <br />                                    | <br />          |
| `NotMediatorExtension.cs`              | **删除**                                    | **Significant** |
| `EntityFramework/IdentityDbContext.cs` | 修改 `using` + 调用链                          | Minor           |
| **Message.Infrastructure**             | <br />                                    | <br />          |
| `NotMediatorExtension.cs`              | **删除**                                    | **Significant** |
| `EntityFramework/MessageDbContext.cs`  | 修改 `using` + 调用链                          | Minor           |
| **Markdown.Infrastructure**            | <br />                                    | <br />          |
| `NotMediatorExtension.cs`              | **删除**                                    | **Significant** |
| `EntityFramework/MarkDownDbContext.cs` | 修改 `using` + 调用链                          | Minor           |
| **FileDev.Infrastructure**             | <br />                                    | <br />          |
| `NotMediatorExtension.cs`              | **删除** + 注意命名空间 `ConsoleApp1`             | **Significant** |
| `EntityFramework/NotFileDbContext.cs`  | 修改 `using` + 调用链                          | Minor           |
| **Video.Infrastructure**               | <br />                                    | <br />          |
| `NotMediatorExtension.cs`              | **删除**                                    | **Significant** |
| `EntityFramework/VideoDbContext.cs`    | 修改 `using` + 调用链                          | Minor           |

***

## 五、风险与影响

1. **`INotMediator`** **vs** **`IDomainEvents`** **接口适配**：`NotMediatorExtension` 当前版本直接访问 `Entity.DomainEventbus`（具体属性），而 `DomainInfrastructure` 的 `MediatorExtensions` 应使用 `IDomainEvents` 接口（DomainCommons 中定义）。需要确认各 Domain 的 Entity 基类是否实现了 `IDomainEvents`。
2. **FileDev.Infrastructure 的命名空间问题**：该项目的 `NotMediatorExtension.cs` 命名空间是 `ConsoleApp1`，属于遗留问题，但也说明该文件处于不稳定状态。
3. **编译依赖链**：修改 `DomainInfrastructure` 后需要验证 `CommonsInitializer` 及所有下游引用项目的编译。

***

## 六、验证步骤

1. 先编译 `DomainInfrastructure`：`dotnet build f:\NotBlog\DomainInfrastructure\DomainInfrastructure.csproj`
2. 编译 `CommonsInitializer`（直接引用 DomainInfrastructure）
3. 逐一编译 5 个 Infrastructure 项目
4. 编译全部上游 Web.API 项目
5. `dotnet build f:\NotBlog\NotBlog.sln` — 全解决方案编译
6. 确认无编译错误和警告

