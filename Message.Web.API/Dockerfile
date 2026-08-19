# syntax=docker/dockerfile:1

# ══════════════════════════════════════════════════════════════
# Message.Web.API 容器镜像
# 构建命令（在解决方案根目录 f:\NotBlog 下执行）：
#   docker build -f Message.Web.API/Dockerfile -t notblog/message-web-api .
# ══════════════════════════════════════════════════════════════

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1) 仅复制项目文件，充分利用 Docker 层缓存（源码变更不触发重新 restore）
COPY ["CacheMemory/CacheMemory.csproj", "CacheMemory/"]
COPY ["Commons/Commons.csproj", "Commons/"]
COPY ["Eventbus/EventBus.csproj", "Eventbus/"]
COPY ["JWToken/JWToken.csproj", "JWToken/"]
COPY ["NotBlog.ServiceDefaults/NotBlog.ServiceDefaults.csproj", "NotBlog.ServiceDefaults/"]
COPY ["Message.Domain/Message.Domain.csproj", "Message.Domain/"]
COPY ["Message.Infrastructure/Message.Infrastructure.csproj", "Message.Infrastructure/"]
COPY ["Message.Web.API/Message.Web.API.csproj", "Message.Web.API/"]

# 2) 还原依赖
RUN dotnet restore "Message.Web.API/Message.Web.API.csproj"

# 3) 复制全部源码
COPY ["CacheMemory/", "CacheMemory/"]
COPY ["Commons/", "Commons/"]
COPY ["Eventbus/", "Eventbus/"]
COPY ["JWToken/", "JWToken/"]
COPY ["NotBlog.ServiceDefaults/", "NotBlog.ServiceDefaults/"]
COPY ["Message.Domain/", "Message.Domain/"]
COPY ["Message.Infrastructure/", "Message.Infrastructure/"]
COPY ["Message.Web.API/", "Message.Web.API/"]

# 4) 发布（--no-restore 复用步骤 2 的还原结果）
RUN dotnet publish "Message.Web.API/Message.Web.API.csproj" \
    -c $BUILD_CONFIGURATION \
    --no-restore \
    -o /app/publish

# ── 运行时阶段 ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Message.Web.API.dll"]
