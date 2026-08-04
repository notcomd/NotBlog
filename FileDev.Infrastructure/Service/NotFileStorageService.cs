using Microsoft.Extensions.Logging;
using Notcomd.Token.JWT.Core;
using Notcomd.Token.JWT.Security;

namespace FileDev.Infrastructure.Service;

public class NotFileStorageService : INotFileStorageService
{
    private readonly NotFileStorageOptions _config;
    private readonly string _rootPath;
    private readonly string _tempChunkFullPath;
    private readonly ILogger<NotFileStorageService> _logger;

    /// <summary>
    /// 路径中允许的分隔符之外的非法字符集合（用于清洗相对路径，保留目录分隔符以支持子目录）。
    /// 注意：不能直接使用 Path.GetInvalidFileNameChars()，因为该方法在 Windows/Linux 上
    /// 均包含路径分隔符（\ /），会破坏子目录结构。
    /// </summary>
    private static readonly char[] InvalidPathChars = Path.GetInvalidFileNameChars()
        .Except(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar })
        .ToArray();

    /// <summary>
    /// fileKey 清洗字符集合：文件名非法字符 + 通配符（* ?），防止 CleanupChunksAsync 的搜索模式匹配到非预期文件。
    /// </summary>
    private static readonly char[] InvalidFileKeyChars = Path.GetInvalidFileNameChars()
        .Concat(new[] { '*', '?' })
        .ToArray();

    /// <summary>
    /// 构造函数（注入配置与日志）
    /// </summary>
    public NotFileStorageService(IOptionsSnapshot<NotFileStorageOptions> configOptions,
        ILogger<NotFileStorageService> logger)
    {
        _config = configOptions.Value;
        _logger = logger;

        // 拼接绝对根目录（基于应用程序基目录）
        _rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _config.StoragePath);
        // 拼接分片临时目录
        _tempChunkFullPath = Path.Combine(_rootPath, _config.TempPath);

        // 初始化目录（根目录+临时分片目录）
        EnsureDirectoryExistsAsync(_rootPath);
        EnsureDirectoryExistsAsync(_tempChunkFullPath);
    }

    /// <summary>
    /// 确保目录存在，不存在则创建
    /// </summary>
    private Task EnsureDirectoryExistsAsync(string dirPath)
    {
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取安全的完整路径（过滤非法字符并校验路径穿越）。
    /// 保留目录分隔符（/ \）以支持子目录结构，仅清洗其他非法字符。
    /// </summary>
    private Task<string> GetSafeFullPathAsync(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("文件相对路径不能为空", nameof(relativePath));

        // 清洗非法字符（保留路径分隔符），防止注入控制字符等
        var safePath = InvalidPathChars
            .Aggregate(relativePath, (current, c) => current.Replace(c.ToString(), "_"));
        // 拼接完整路径后用 Path.GetFullPath 规范化，防止 ".." 等相对路径逃逸（路径穿越，S-16）
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, safePath));
        // 校验规范化后的路径必须位于存储根目录内（Path.GetRelativePath 兼容 Windows/Linux 分隔符差异）
        var relative = Path.GetRelativePath(_rootPath, fullPath);
        var firstSegment = relative.Split(Path.DirectorySeparatorChar, 2)[0];
        if (relative == "." || firstSegment == ".." || Path.IsPathRooted(relative))
        {
            // S-16：路径越界直接拒绝，错误消息不暴露绝对路径
            throw new ArgumentException("非法文件路径，禁止越出存储根目录");
        }

        return Task.FromResult(fullPath);
    }

    /// <summary>
    /// 获取分片临时文件路径
    /// </summary>
    private Task<string> GetChunkTempPathAsync(string fileKey, int chunkIndex)
    {
        var safeFileKey = SanitizeFileKey(fileKey);
        return Task.FromResult(Path.Combine(_tempChunkFullPath, $"{safeFileKey}_chunk_{chunkIndex}"));
    }

    /// <summary>
    /// 清洗 fileKey：替换文件名非法字符和通配符（* ?）为下划线，
    /// 确保 GetChunkTempPathAsync 与 CleanupChunksAsync 使用一致的清洗逻辑。
    /// </summary>
    private static string SanitizeFileKey(string fileKey)
    {
        return InvalidFileKeyChars.Aggregate(fileKey, (current, c) => current.Replace(c.ToString(), "_"));
    }

    #region 基础文件操作（兼容原有逻辑）

    public async Task<NotFileStorageResponse> SaveAsync(NotFileStorageRequest request)
    {
        try
        {
            var fullPath = await GetSafeFullPathAsync(request.FileRelativePath);
            var directory = Path.GetDirectoryName(fullPath);
            await EnsureDirectoryExistsAsync(directory ?? throw new ArgumentException("无效的相对路径"));

            // 覆盖检查
            if (File.Exists(fullPath) && !request.Overwrite)
            {
                return new NotFileStorageResponse
                {
                    Success = false,
                    // S-16：错误消息不包含绝对路径，仅暴露相对路径
                    ErrorMessage = $"文件已存在且禁止覆盖：{request.FileRelativePath}",
                    FullPath = string.Empty
                };
            }

            // 写入文件并校验（如果传入了哈希值）
            await File.WriteAllBytesAsync(fullPath, request.FileContent);

            // 可选：验证文件哈希（如果请求中传入了预期哈希，统一使用 SHA256，见 F-09.5）
            if (!string.IsNullOrEmpty(request.ExpectedHash))
            {
                bool hashMatch = HashHelper.VerifyFileHash(fullPath, request.ExpectedHash, AlgorithmType.SHA256);
                if (!hashMatch)
                {
                    // 必须在删除文件之前计算实际哈希，否则文件已删除会返回空字符串
                    var actualHash = HashHelper.ComputeFileHash(fullPath, AlgorithmType.SHA256);
                    File.Delete(fullPath); // 校验失败删除文件
                    // S-16：哈希不一致仅记日志与相对路径，不对外暴露绝对路径
                    _logger.LogWarning("保存文件哈希校验失败 FileRelativePath={FileRelativePath} ExpectedHash={ExpectedHash}",
                        request.FileRelativePath, request.ExpectedHash);
                    return new NotFileStorageResponse
                    {
                        Success = false,
                        ErrorMessage =
                            $"文件哈希校验失败，预期：{request.ExpectedHash}，实际：{actualHash}",
                        FullPath = string.Empty
                    };
                }
            }

            return new NotFileStorageResponse
            {
                Success = true,
                FullPath = fullPath,
                FileSize = new FileInfo(fullPath).Length,
                ActualHash = HashHelper.ComputeFileHash(fullPath, AlgorithmType.SHA256)
            };
        }
        catch (Exception ex)
        {
            // S-16：异常详情与绝对路径写入日志，对外仅返回通用文案
            _logger.LogError(ex, "保存文件失败 FileRelativePath={FileRelativePath}", request.FileRelativePath);
            return new NotFileStorageResponse
            {
                Success = false,
                ErrorMessage = "保存文件失败",
                FullPath = string.Empty
            };
        }
    }

    public async Task<NotFileStorageResponse> DeleteAsync(string fileRelativePath)
    {
        try
        {
            var fullPath = await GetSafeFullPathAsync(fileRelativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return new NotFileStorageResponse
                {
                    Success = true,
                    FullPath = fullPath
                };
            }

            return new NotFileStorageResponse
            {
                Success = false,
                // S-16：错误消息不包含绝对路径
                ErrorMessage = $"文件不存在：{fileRelativePath}",
                FullPath = string.Empty
            };
        }
        catch (Exception ex)
        {
            // S-16：异常详情与绝对路径写入日志，对外仅返回通用文案
            _logger.LogError(ex, "删除文件失败 FileRelativePath={FileRelativePath}", fileRelativePath);
            return new NotFileStorageResponse
            {
                Success = false,
                ErrorMessage = "删除文件失败"
            };
        }
    }

    public async Task<(byte[] Content, NotFileStorageResponse Response)> GetContentAsync(string fileRelativePath)
    {
        try
        {
            var fullPath = await GetSafeFullPathAsync(fileRelativePath);
            if (!File.Exists(fullPath))
            {
                return (null, new NotFileStorageResponse
                {
                    Success = false,
                    // S-16：错误消息不包含绝对路径
                    ErrorMessage = $"文件不存在：{fileRelativePath}",
                    FullPath = string.Empty
                })!;
            }

            var content = File.ReadAllBytes(fullPath);
            return (content, new NotFileStorageResponse
            {
                Success = true,
                FullPath = fullPath,
                FileSize = content.Length,
                ActualHash = HashHelper.ComputeHash(content, AlgorithmType.SHA256)
            });
        }
        catch (Exception ex)
        {
            // S-16：异常详情与绝对路径写入日志，对外仅返回通用文案
            _logger.LogError(ex, "读取文件失败 FileRelativePath={FileRelativePath}", fileRelativePath);
            return (null, new NotFileStorageResponse
            {
                Success = false,
                ErrorMessage = "读取文件失败"
            })!;
        }
    }

    public async Task<bool> ExistsAsync(string fileRelativePath)
    {
        try
        {
            return File.Exists(await GetSafeFullPathAsync(fileRelativePath));
        }
        catch (Exception ex)
        {
            // S-16：非法路径视为不存在，避免路径穿越校验异常向上传播
            _logger.LogWarning(ex, "检查文件存在性失败 FileRelativePath={FileRelativePath}", fileRelativePath);
            return false;
        }
    }

    /// <summary>
    /// 流式获取文件内容（S-09：避免大文件整读入内存）
    /// </summary>
    public async Task<(Stream? Content, NotFileStorageResponse Response)> GetContentStreamAsync(string fileRelativePath)
    {
        try
        {
            var fullPath = await GetSafeFullPathAsync(fileRelativePath);
            if (!File.Exists(fullPath))
            {
                return (null, new NotFileStorageResponse
                {
                    Success = false,
                    // S-16：错误消息不包含绝对路径
                    ErrorMessage = $"文件不存在：{fileRelativePath}",
                    FullPath = string.Empty
                })!;
            }

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                64 * 1024, FileOptions.Asynchronous);
            return (stream, new NotFileStorageResponse
            {
                Success = true,
                FullPath = fullPath,
                FileSize = stream.Length
            });
        }
        catch (Exception ex)
        {
            // S-16：异常详情与绝对路径写入日志，对外仅返回通用文案
            _logger.LogError(ex, "读取文件失败 FileRelativePath={FileRelativePath}", fileRelativePath);
            return (null, new NotFileStorageResponse
            {
                Success = false,
                ErrorMessage = "读取文件失败"
            })!;
        }
    }

    /// <summary>
    /// 清理某上传任务的全部临时分片文件（S-09）
    /// </summary>
    public Task CleanupChunksAsync(string fileKey)
    {
        var safeFileKey = SanitizeFileKey(fileKey);
        var pattern = $"{safeFileKey}_chunk_*";
        foreach (var chunkFile in Directory.EnumerateFiles(_tempChunkFullPath, pattern))
        {
            try
            {
                File.Delete(chunkFile);
            }
            catch
            {
                // 单个分片删除失败不阻断整体清理，交由过期清理任务兜底
            }
        }

        return Task.CompletedTask;
    }

    #endregion

    #region 分片上传核心方法

    /// <summary>
    /// 上传单个分片（带 SHA256 校验）
    /// </summary>
    /// <param name="fileKey">文件唯一标识（如：docs/2026/bigfile.zip）</param>
    /// <param name="chunkIndex">分片索引（从0开始）</param>
    /// <param name="chunkContent">分片字节内容</param>
    /// <param name="chunkHash">分片预期哈希值（用于校验）</param>
    /// <returns>分片上传结果</returns>
    public async Task<NotFileStorageResponse> UploadChunkAsync(string fileKey, int chunkIndex, byte[] chunkContent,
        string? chunkHash = null)
    {
        try
        {
            // 1. 校验分片哈希（如果传入，统一 SHA256，见 F-09.5）
            if (!string.IsNullOrEmpty(chunkHash))
            {
                bool isChunkValid = HashHelper.VerifyHash(chunkContent, chunkHash, AlgorithmType.SHA256);
                if (!isChunkValid)
                {
                    // S-16：哈希不一致仅记日志，不对外暴露绝对路径
                    _logger.LogWarning("分片哈希校验失败 FileKey={FileKey} ChunkIndex={ChunkIndex}",
                        fileKey, chunkIndex);
                    return new NotFileStorageResponse
                    {
                        Success = false,
                        ErrorMessage = $"分片{chunkIndex}哈希校验失败",
                        FullPath = string.Empty
                    };
                }
            }

            // 2. 保存分片到临时目录
            var chunkTempPath = await GetChunkTempPathAsync(fileKey, chunkIndex);
            await File.WriteAllBytesAsync(chunkTempPath, chunkContent);

            return new NotFileStorageResponse
            {
                Success = true,
                FullPath = chunkTempPath,
                FileSize = chunkContent.Length,
                ActualHash = HashHelper.ComputeHash(chunkContent, AlgorithmType.SHA256)
            };
        }
        catch (Exception ex)
        {
            // S-16：异常详情与绝对路径写入日志，对外仅返回通用文案
            _logger.LogError(ex, "上传分片失败 FileKey={FileKey} ChunkIndex={ChunkIndex}", fileKey, chunkIndex);
            return new NotFileStorageResponse
            {
                Success = false,
                ErrorMessage = $"上传分片{chunkIndex}失败"
            };
        }
    }

    /// <summary>
    /// 合并分片为完整文件
    /// </summary>
    /// <param name="fileKey">文件唯一标识（最终存储的相对路径）</param>
    /// <param name="totalChunks">总分片数</param>
    /// <param name="expectedFileHash">文件整体预期哈希（可选，为 null 时跳过哈希校验）</param>
    /// <param name="overwrite">是否覆盖已存在文件</param>
    /// <returns>合并结果</returns>
    public async Task<NotFileStorageResponse> MergeChunksAsync(string fileKey, int totalChunks,
        string? expectedFileHash = null, bool overwrite = true)
    {
        try
        {
            // 1. 检查所有分片是否存在
            var chunkPaths = new List<string>();
            for (int i = 0; i < totalChunks; i++)
            {
                var chunkPath = await GetChunkTempPathAsync(fileKey, i);
                if (!File.Exists(chunkPath))
                {
                    return new NotFileStorageResponse
                    {
                        Success = false,
                        // S-16：错误消息不包含绝对路径，仅提示缺失的分片序号
                        ErrorMessage = $"分片{i}缺失",
                        FullPath = string.Empty
                    };
                }

                chunkPaths.Add(chunkPath);
            }

            // 2. 合并分片到最终文件路径
            var finalFilePath = await GetSafeFullPathAsync(fileKey);
            var finalFileDir = Path.GetDirectoryName(finalFilePath);
            await EnsureDirectoryExistsAsync(finalFileDir ?? throw new ArgumentException("无效的文件路径"));

            // 覆盖检查
            if (File.Exists(finalFilePath) && !overwrite)
            {
                return new NotFileStorageResponse
                {
                    Success = false,
                    // S-16：错误消息不包含绝对路径，仅暴露相对路径 fileKey
                    ErrorMessage = $"最终文件已存在且禁止覆盖：{fileKey}",
                    FullPath = string.Empty
                };
            }

            // 3. 写入合并后的文件（流式写入，避免大文件占满内存）
            await using (var finalStream = new FileStream(finalFilePath, FileMode.Create, FileAccess.Write))
            {
                foreach (var chunkPath in chunkPaths)
                {
                    await using var chunkStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read);
                    await chunkStream.CopyToAsync(finalStream);
                }
            }

            // 4. 校验最终文件哈希（如果传入，统一 SHA256，见 F-09.5）
            string actualFileHash = HashHelper.ComputeFileHash(finalFilePath, AlgorithmType.SHA256);
            if (!string.IsNullOrEmpty(expectedFileHash))
            {
                var isFileValid = HashHelper.VerifyFileHash(finalFilePath, expectedFileHash, AlgorithmType.SHA256);
                if (!isFileValid)
                {
                    File.Delete(finalFilePath); // 校验失败删除文件
                    // S-16：哈希不一致仅记日志，不对外暴露绝对路径
                    _logger.LogWarning("文件合并后哈希校验失败 FileKey={FileKey} ExpectedHash={ExpectedHash}",
                        fileKey, expectedFileHash);
                    return new NotFileStorageResponse
                    {
                        Success = false,
                        ErrorMessage = $"文件合并后哈希校验失败，预期：{expectedFileHash}，实际：{actualFileHash}",
                        FullPath = string.Empty
                    };
                }
            }

            // 5. 删除临时分片文件
            foreach (var chunkPath in chunkPaths)
            {
                File.Delete(chunkPath);
            }

            return new NotFileStorageResponse
            {
                Success = true,
                FullPath = finalFilePath,
                FileSize = new FileInfo(finalFilePath).Length,
                ActualHash = actualFileHash
            };
        }
        catch (Exception ex)
        {
            // S-16：异常详情与绝对路径写入日志，对外仅返回通用文案
            _logger.LogError(ex, "合并分片失败 FileKey={FileKey}", fileKey);
            return new NotFileStorageResponse
            {
                Success = false,
                ErrorMessage = "合并分片失败",
                FullPath = string.Empty
            };
        }
    }

    /// <summary>
    /// 获取文件分片数量（根据文件大小和配置的分片大小）
    /// </summary>
    /// <param name="fileSize">文件总大小（字节）</param>
    /// <returns>总分片数</returns>
    public Task<int> GetTotalChunkCountAsync(long fileSize)
    {
        if (fileSize <= 0) return Task.FromResult(1);
        return Task.FromResult((int)Math.Ceiling((double)fileSize / _config.ChunkFileSize));
    }

    #endregion
}
