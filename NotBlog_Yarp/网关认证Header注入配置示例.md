# NotBlog 网关认证 Header 注入配置示例

> **对应文档**: `Message.Domain\Tweet功能集成开发文档.md` — 3.1 认证规范  
> **原则**: 网关统一验证 JWT → 提取 UserId + Roles → 注入 Header `X-User-Id` / `X-User-Roles` → 下游服务仅做所有权校验

---

## 1. Program.cs — 网关入口

```csharp
// f:\NotBlog\NotBlog_Yarp\Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Yarp.ReverseProxy.Transforms.Builder;

var builder = WebApplication.CreateBuilder(args);

// ========== 1. JWT 认证配置 ==========
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSection["SecretKey"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero                      // 无时钟偏移容差
        };
    });

builder.Services.AddAuthorization();

// ========== 2. YARP 反向代理 + 自定义 Header 注入 Transform ==========
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(context =>
    {
        // 为所有路由添加用户身份 Header 注入
        context.AddUserContextTransform();
    });

// ========== 3. 注册自定义 Transform Provider ==========
builder.Services.AddSingleton<ITransformProvider, UserContextTransformProvider>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();
app.Run();
```

---

## 2. 自定义 Transform — X-User-Id / X-User-Roles 注入逻辑

```csharp
// f:\NotBlog\NotBlog_Yarp\Transforms\UserContextTransform.cs

using System.Security.Claims;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

/// <summary>
/// 从 JWT Claims 中提取 UserId 和 Roles，
/// 注入到下游请求的 Header: X-User-Id, X-User-Roles
/// </summary>
public class UserContextTransform : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            // 提取用户 GUID（假设 JWT claim 类型为 "sub" 或 "nameid"）
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value
                      ?? user.FindFirst("id")?.Value;

            // 提取角色列表
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value);

            if (!string.IsNullOrEmpty(userId))
            {
                // 移除客户端可能伪造的 Header
                context.ProxyRequest.Headers.Remove("X-User-Id");
                context.ProxyRequest.Headers.Remove("X-User-Roles");

                // 注入可信 Header
                context.ProxyRequest.Headers.Add("X-User-Id", userId);
                context.ProxyRequest.Headers.Add("X-User-Roles", string.Join(",", roles));
            }
        }
        else
        {
            // 未认证请求：清除可能存在的伪造 Header
            context.ProxyRequest.Headers.Remove("X-User-Id");
            context.ProxyRequest.Headers.Remove("X-User-Roles");
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// 注册 Transform 到所有路由
/// </summary>
public class UserContextTransformProvider : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context) { }

    public void ValidateCluster(TransformClusterValidationContext context) { }

    public void Apply(TransformBuilderContext context)
    {
        // 为每个路由添加用户上下文注入
        context.AddRequestTransform(new UserContextTransform());
    }
}

/// <summary>
/// Transform 扩展方法
/// </summary>
public static class UserContextTransformExtensions
{
    public static TransformBuilderContext AddUserContextTransform(
        this TransformBuilderContext context)
    {
        context.AddRequestTransform(new UserContextTransform());
        return context;
    }
}
```

---

## 3. appsettings.json — 完整网关配置

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Yarp.ReverseProxy": "Information"
    }
  },

  "JwtSettings": {
    "SecretKey": "your-super-secret-key-at-least-32-characters-long!",
    "Issuer": "NotBlog.Identity",
    "Audience": "NotBlog"
  },

  "ReverseProxy": {
    "Routes": {
      "identity-service": {
        "ClusterId": "identity-cluster",
        "Match": {
          "Path": "/api/identity/{**catch-all}"
        },
        "Transforms": [
          { "PathRemovePrefix": "/api/identity" }
        ]
      },
      "message-service": {
        "ClusterId": "message-cluster",
        "Match": {
          "Path": "/api/{controller:tweets|comments|reports|audit}/{**catch-all}"
        }
      },
      "message-sessions": {
        "ClusterId": "message-cluster",
        "Match": {
          "Path": "/api/{controller:sessions|friends|groups|files|messages}/{**catch-all}"
        }
      },
      "filedev-service": {
        "ClusterId": "filedev-cluster",
        "Match": {
          "Path": "/api/filestorage/{**catch-all}"
        }
      },
      "markdown-service": {
        "ClusterId": "markdown-cluster",
        "Match": {
          "Path": "/api/markdown/{**catch-all}"
        },
        "Transforms": [
          { "PathRemovePrefix": "/api/markdown" }
        ]
      },
      "video-service": {
        "ClusterId": "video-cluster",
        "Match": {
          "Path": "/api/video/{**catch-all}"
        },
        "Transforms": [
          { "PathRemovePrefix": "/api/video" }
        ]
      }
    },

    "Clusters": {
      "identity-cluster": {
        "Destinations": {
          "identity-dest": {
            "Address": "https://localhost:9091"
          }
        }
      },
      "message-cluster": {
        "Destinations": {
          "message-dest": {
            "Address": "https://localhost:9092"
          }
        }
      },
      "filedev-cluster": {
        "Destinations": {
          "filedev-dest": {
            "Address": "https://localhost:9093"
          }
        }
      },
      "markdown-cluster": {
        "Destinations": {
          "markdown-dest": {
            "Address": "https://localhost:9094"
          }
        }
      },
      "video-cluster": {
        "Destinations": {
          "video-dest": {
            "Address": "https://localhost:9095"
          }
        }
      }
    }
  }
}
```

---

## 4. 可选：匿名路由支持（无需认证的公开接口）

部分接口允许匿名访问（如获取公开推文、热门推文），可在 YARP 中配置匿名路由策略：

```csharp
// Program.cs 补充 — 匿名路由配置

builder.Services.AddAuthorization(options =>
{
    // 默认策略：需要认证
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // 匿名策略：无需认证的端点
    options.AddPolicy("AnonymousPolicy", policy =>
        policy.RequireAssertion(_ => true));
});

// 路由级匿名配置（在 appsettings.json 的 Route 节点中）
// "AnonymousRoutes": {
//   "message-service-public": {
//     "ClusterId": "message-cluster",
//     "AuthorizationPolicy": "AnonymousPolicy",
//     "Match": {
//       "Path": "/api/tweets/trending",
//       "Methods": [ "GET" ]
//     }
//   }
// }
```

---

## 5. 请求流转示意

```
客户端                        API Gateway (YARP)                     下游服务 (Message.Web.API)
  │                                │                                        │
  │  GET /api/tweets/trending       │                                        │
  │  Authorization: Bearer <JWT>   │                                        │
  │ ──────────────────────────────▶│                                        │
  │                                │  JWT 验证 (JwtBearerMiddleware)         │
  │                                │  ├─ 解析签名、Issuer、Audience          │
  │                                │  ├─ 验证过期时间                        │
  │                                │  └─ 提取 Claims:                        │
  │                                │       sub=user-guid-a                   │
  │                                │       role=User                         │
  │                                │                                        │
  │                                │  UserContextTransform.ApplyAsync()      │
  │                                │  ├─ 注入 Header:                        │
  │                                │  │    X-User-Id: user-guid-a           │
  │                                │  │    X-User-Roles: User               │
  │                                │  └─ 移除客户端伪造 Header               │
  │                                │                                        │
  │                                │  GET /api/tweets/trending               │
  │                                │  X-User-Id: user-guid-a                │
  │                                │  X-User-Roles: User                    │
  │                                │ ──────────────────────────────────────▶│
  │                                │                                        │
  │                                │                                        │  UserContextMiddleware
  │                                │                                        │  提取 X-User-Id/X-User-Roles
  │                                │                                        │  设置 ICurrentUserService
  │                                │                                        │
  │                                │                                        │  Provider 层处理业务
  │                                │                                        │
  │                                │  ◀── 200 OK + JSON ───────────────────│
  │  ◀── 200 OK + JSON ───────────│                                        │
```

---

## 6. 下游服务 Middleware 配套代码

网关注入 Header 后，下游服务需要对应的 Middleware 来消费这些 Header：

```csharp
// f:\NotBlog\Message.Web.API\Middleware\UserContextMiddleware.cs
namespace Message.Web.API.Middleware;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser)
    {
        var userIdHeader = context.Request.Headers["X-User-Id"].FirstOrDefault();
        var rolesHeader = context.Request.Headers["X-User-Roles"].FirstOrDefault();

        if (Guid.TryParse(userIdHeader, out var userGuid))
        {
            var roles = rolesHeader?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       ?? Array.Empty<string>();
            currentUser.SetUser(userGuid, roles);
        }

        await _next(context);
    }
}

// Program.cs 注册
// app.UseMiddleware<UserContextMiddleware>();
```

---

> **注意事项**:
> 1. `X-User-Id` 和 `X-User-Roles` 由网关注入，网关在注入前**必须移除**客户端请求中已存在的同名 Header，防止伪造
> 2. JWT SecretKey 等敏感信息应通过环境变量或 Secrets Manager 管理，不硬编码在配置文件中
> 3. 集群目标地址 `localhost:909x` 应在 Aspire AppHost 中通过 `.WithReference()` 建立服务间引用，由 Aspire 自动注入实际地址
