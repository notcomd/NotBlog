using FileDev.Domain.Dto.Request;
using FileDev.Domain.Dto.Response;
using FileDev.Domain.IServices;
using FileDev.Domain.Options;
using Notcomd.Token.JWT;
using Microsoft.Extensions.Options;

namespace FileDev.Infrastructure.Service;

public class NotFileStorageService: INotFileStorageService
{
    private readonly NotFileStorageOptions _config;
    private readonly string _rootPath;
    private readonly string _tempChunkFullPath;

    /// <summary>
    /// 构造函数（注入配置）
    /// </summary>
    public NotFileStorageService(IOptions<NotFileStorageOptions> configOptions)
    {
        _config = configOptions.Value;

        // 拼接绝对根目录（基于应用程序基目录）
        _rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _config.StoragePath);
        // 拼接分片临时目录
        _tempChunkFullPath = Path.Combine(_rootPath, _config.TempPath);

        // 初始化目录（根目录+临时分片目录）
        EnsureDirectoryExists(_rootPath);
        EnsureDirectoryExists(_tempChunkFullPath);
    }

    /// <summary>
    /// 确保目录存在，不存在则创建
    /// </summary>
    private void EnsureDirectoryExists(string dirPath)
    {
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
    }

    /// <summary>
    /// 获取安全的完整路径（过滤非法字符）
    /// </summary>
    private string GetSafeFullPath(string relativePath)
    {
        var safePath = Path.GetInvalidFileNameChars()
            .Aggregate(relativePath, (current, c) => current.Replace(c.ToString(), "_"));
        return Path.Combine(_rootPath, safePath);
    }

    /// <summary>
    /// 获取分片临时文件路径
    /// </summary>
    private string GetChunkTempPath(string fileKey, int chunkIndex)
    {
        var safeFileKey = Path.GetInvalidFileNameChars()
            .Aggregate(fileKey, (current, c) => current.Replace(c.ToString(), "_"));
        return Path.Combine(_tempChunkFullPath, $"{safeFileKey}_chunk_{chunkIndex}");
    }

    #region 基础文件操作（兼容原有逻辑）
    public NotFileStorageResponse Save(NotFileStorageRequest request)
    {
        try
        {
            var fullPath = GetSafeFullPath(request.FileRelativePath);
            var directory = Path.GetDirectoryName(fullPath);
            EnsureDirectoryExists(directory??throw new ArgumentException("无效的相对路径"));

            // 覆盖检查
            if (File.Exists(fullPath) && !request.Overwrite)
            {
                return new NotFileStorageResponse
                {
                    Success = false,
                    ErrorMessage = $"文件已存在且禁止覆盖：{fullPath}",
                    FullPath = fullPath
                };
            }

            // 写入文件并校验（如果传入了哈希值）
            File.WriteAllBytes(fullPath, request.FileContent);
            
            // 可选：验证文件哈希（如果请求中传入了预期哈希）
            if (!string.IsNullOrEmpty(request.ExpectedHash))
            {
                bool hashMatch = HashHelper.VerifyFileHash(fullPath, request.ExpectedHash, _config.HashAlgorithm);
                if (!hashMatch)
                {
                    File.Delete(fullPath); // 校验失败删除文件
                    return new NotFileStorageResponse
                    {
                        Success = false,
                        ErrorMessage = $"文件哈希校验失败，预期：{request.ExpectedHash}，实际：{HashHelper.ComputeFileHash(fullPath, _config.HashAlgorithm)}",
                        FullPath = fullPath
                    };
                }
            }

            return new NotFileStorageResponse
            {
                Success = true,
                FullPath = fullPath,
                FileSize = new FileInfo(fullPath).Length,
                ActualHash = HashHelper.ComputeFileHash(fullPath, _config.HashAlgorithm)
            };
        }
        catch (Exception ex)
        {
            return new NotFileStorageResponse
            {
                Success = false,
                ErrorMessage = $"保存文件失败：{ex.Message}",
                FullPath = string.Empty
            };
        }
    }

    public NotFileStorageResponse Delete(string fileRelativePath)
    {
        try
        {
            var fullPath = GetSafeFullPath(fileRelativePath);
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
                ErrorMessage = $"文件不存在：{fullPath}",
                FullPath = fullPath
            };
        }
        catch (Exception ex)
        {
            return new NotFileStorageResponse
            {
                Success = false,
                ErrorMessage = $"删除文件失败：{ex.Message}"
            };
        }
    }

    public (byte[] Content, NotFileStorageResponse Response) GetContent(string fileRelativePath)
    {
        try
        {
            var fullPath = GetSafeFullPath(fileRelativePath);
            if (!File.Exists(fullPath))
            {
                return (null, new NotFileStorageResponse
                {
                    Success = false,
                    ErrorMessage = $"文件不存在：{fullPath}",
                    FullPath = fullPath
                })!;
            }

            var content = File.ReadAllBytes(fullPath);
            return (content, new NotFileStorageResponse
            {
                Success = true,
                FullPath = fullPath,
                FileSize = content.Length,
                ActualHash = HashHelper.ComputeHash(content, _config.HashAlgorithm)
            });
        }
        catch (Exception ex)
        {
            return (null, new NotFileStorageResponse
            {
                Success = false,
                ErrorMessage = $"读取文件失败：{ex.Message}"
            })!;
        }
    }

    public bool Exists(string fileRelativePath)
    {
        return File.Exists(GetSafeFullPath(fileRelativePath));
    }
    #endregion

    #region 分片上传核心方法
    /// <summary>
    /// 上传单个分片（带MD5校验）
    /// </summary>
    /// <param name="fileKey">文件唯一标识（如：docs/2026/bigfile.zip）</param>
    /// <param name="chunkIndex">分片索引（从0开始）</param>
    /// <param name="chunkContent">分片字节内容</param>
    /// <param name="chunkHash">分片预期哈希值（用于校验）</param>
    /// <returns>分片上传结果</returns>
    public NotFileStorageResponse UploadChunk(string fileKey, int chunkIndex, byte[] chunkContent, string? chunkHash = null)
    {
        try
        {
            // 1. 校验分片哈希（如果传入）
            if (!string.IsNullOrEmpty(chunkHash))
            {
                bool isChunkValid = HashHelper.VerifyHash(chunkContent, chunkHash, _config.HashAlgorithm);
                if (!isChunkValid)
                {
                    return new NotFileStorageResponse
                    {
                        Success = false,
                        ErrorMessage = $"分片{chunkIndex}哈希校验失败",
                        FullPath = GetChunkTempPath(fileKey, chunkIndex)
                    };
                }
            }

            // 2. 保存分片到临时目录
            var chunkTempPath = GetChunkTempPath(fileKey, chunkIndex);
            File.WriteAllBytes(chunkTempPath, chunkContent);

            return new NotFileStorageResponse
            {
                Success = true,
                FullPath = chunkTempPath,
                FileSize = chunkContent.Length,
                ActualHash = HashHelper.ComputeHash(chunkContent, _config.HashAlgorithm)
            };
        }
        catch (Exception ex)
        {
            return new NotFileStorageResponse
            {
                Success = false,
                ErrorMessage = $"上传分片{chunkIndex}失败：{ex.Message}"
            };
        }
    }

    /// <summary>
    /// 合并分片为完整文件
    /// </summary>
    /// <param name="fileKey">文件唯一标识（最终存储的相对路径）</param>
    /// <param name="totalChunks">总分片数</param>
    /// <param name="expectedFileHash">文件整体预期哈希（可选）</param>
    /// <param name="overwrite">是否覆盖已存在文件</param>
    /// <returns>合并结果</returns>
    public NotFileStorageResponse MergeChunks(string fileKey, int totalChunks, string? expectedFileHash = null, bool overwrite = true)
    {
        try
        {
            // 1. 检查所有分片是否存在
            var chunkPaths = new List<string>();
            for (int i = 0; i < totalChunks; i++)
            {
                var chunkPath = GetChunkTempPath(fileKey, i);
                if (!File.Exists(chunkPath))
                {
                    return new NotFileStorageResponse
                    {
                        Success = false,
                        ErrorMessage = $"分片{i}缺失，路径：{chunkPath}",
                        FullPath = GetSafeFullPath(fileKey)
                    };
                }
                chunkPaths.Add(chunkPath);
            }

            // 2. 合并分片到最终文件路径
            var finalFilePath = GetSafeFullPath(fileKey);
            var finalFileDir = Path.GetDirectoryName(finalFilePath);
            EnsureDirectoryExists(finalFileDir??throw new ArgumentException("无效的文件路径"));

            // 覆盖检查
            if (File.Exists(finalFilePath) && !overwrite)
            {
                return new NotFileStorageResponse
                {
                    Success = false,
                    ErrorMessage = $"最终文件已存在且禁止覆盖：{finalFilePath}",
                    FullPath = finalFilePath
                };
            }

            // 3. 写入合并后的文件（流式写入，避免大文件占满内存）
            using (var finalStream = new FileStream(finalFilePath, FileMode.Create, FileAccess.Write))
            {
                foreach (var chunkPath in chunkPaths)
                {
                    using (var chunkStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read))
                    {
                        chunkStream.CopyTo(finalStream);
                    }
                }
            }

            // 4. 校验最终文件哈希（如果传入）
            string actualFileHash = HashHelper.ComputeFileHash(finalFilePath, _config.HashAlgorithm);
            if (!string.IsNullOrEmpty(expectedFileHash))
            {
                var isFileValid = HashHelper.VerifyFileHash(finalFilePath, expectedFileHash, _config.HashAlgorithm);
                if (!isFileValid)
                {
                    File.Delete(finalFilePath); // 校验失败删除文件
                    return new NotFileStorageResponse
                    {
                        Success = false,
                        ErrorMessage = $"文件合并后哈希校验失败，预期：{expectedFileHash}，实际：{actualFileHash}",
                        FullPath = finalFilePath
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
            return new NotFileStorageResponse
            {
                Success = false,
                ErrorMessage = $"合并分片失败：{ex.Message}",
                FullPath = GetSafeFullPath(fileKey)
            };
        }
    }

    /// <summary>
    /// 获取文件分片数量（根据文件大小和配置的分片大小）
    /// </summary>
    /// <param name="fileSize">文件总大小（字节）</param>
    /// <returns>总分片数</returns>
    public int GetTotalChunkCount(long fileSize)
    {
        if (fileSize <= 0) return 1;
        return (int)Math.Ceiling((double)fileSize / _config.ChunkFileSize);
    }
    #endregion
}