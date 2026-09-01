# ══════════════════════════════════════════════════════════════
# NotBlog_Yarp 网关容器镜像
# 方案：宿主机已恢复依赖缓存，发布逻辑在宿主机本地执行（见 publish 说明），
#       Docker 在此仅 COPY 宿主机 publish 产物，规避容器内访问 NuGet 源超时。
# 发布命令（解决方案根目录 f:\NotBlog 下执行）：
#   dotnet publish NotBlog_Yarp/NotBlog_Yarp.csproj -c Release -o publish/yarp
# 构建命令：
#   docker build -f NotBlog_Yarp/Dockerfile -t notblog/notblog-yarp-gateway:latest .
# ══════════════════════════════════════════════════════════════

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-noble-amd64 AS final
WORKDIR /app
# 从宿主机发布产物复制
COPY publish/yarp/ .
EXPOSE 8085
ENTRYPOINT ["dotnet", "NotBlog_Yarp.dll"]