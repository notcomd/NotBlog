# syntax=docker/dockerfile:1

# ══════════════════════════════════════════════════════════════
# FileDev.Web.API 容器镜像
# 构建命令（在解决方案根目录 f:\NotBlog 下执行）：
#   docker build -f FileDev.Web.API/Dockerfile -t notblog/filedev-web-api .
# ══════════════════════════════════════════════════════════════

FROM mcr.microsoft.com/dotnet/sdk:10.0.400-noble-amd64 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1) 仅复制项目文件，充分利用 Docker 层缓存（源码变更不触发重新 restore）
COPY ["CacheMemory/CacheMemory.csproj", "CacheMemory/"]
COPY ["Commons/Commons.csproj", "Commons/"]
COPY ["Eventbus/EventBus.csproj", "Eventbus/"]
COPY ["JWToken/JWToken.csproj", "JWToken/"]
COPY ["NotBlog.ServiceDefaults/NotBlog.ServiceDefaults.csproj", "NotBlog.ServiceDefaults/"]
COPY ["FileDev.Domain/FileDev.Domain.csproj", "FileDev.Domain/"]
COPY ["FileDev.Infrastructure/FileDev.Infrastructure.csproj", "FileDev.Infrastructure/"]
COPY ["FileDev.Web.API/FileDev.Web.API.csproj", "FileDev.Web.API/"]

# 2) 还原依赖
RUN dotnet restore "FileDev.Web.API/FileDev.Web.API.csproj"

# 3) 复制全部源码
COPY ["CacheMemory/", "CacheMemory/"]
COPY ["Commons/", "Commons/"]
COPY ["Eventbus/", "Eventbus/"]
COPY ["JWToken/", "JWToken/"]
COPY ["NotBlog.ServiceDefaults/", "NotBlog.ServiceDefaults/"]
COPY ["FileDev.Domain/", "FileDev.Domain/"]
COPY ["FileDev.Infrastructure/", "FileDev.Infrastructure/"]
COPY ["FileDev.Web.API/", "FileDev.Web.API/"]

# 4) 发布（--no-restore 复用步骤 2 的还原结果）
RUN dotnet publish "FileDev.Web.API/FileDev.Web.API.csproj" \
    -c $BUILD_CONFIGURATION \
    --no-restore \
    -o /app/publish

# ── 运行时阶段 ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-noble-amd64 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8081
ENTRYPOINT ["dotnet", "FileDev.Web.API.dll"]
