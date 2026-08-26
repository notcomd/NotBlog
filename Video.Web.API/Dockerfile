# syntax=docker/dockerfile:1

# ══════════════════════════════════════════════════════════════
# Video.Web.API 容器镜像
# 方案：宿主机已恢复依赖缓存，发布逻辑在宿主机本地执行（见 publish 说明），
#       Docker 在此仅 COPY 宿主机 publish 产物，规避容器内访问 NuGet 源超时。
# 发布命令（解决方案根目录 f:\NotBlog 下执行）：
#   dotnet publish Video.Web.API/Video.Web.API.csproj -c Release -o publish/video
# 构建命令：
#   docker build -f Video.Web.API/Dockerfile -t notblog/video-web-api:latest .
# ══════════════════════════════════════════════════════════════

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-noble-amd64 AS final
WORKDIR /app
# 从宿主机发布产物复制（clean 确保可重复构建）
COPY publish/video/ .
EXPOSE 8085
ENTRYPOINT ["dotnet", "Video.Web.API.dll"]
