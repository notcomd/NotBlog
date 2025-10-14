using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DomainCommon;

using FileDev.Domain.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileDev.Domain.DomainService
{
    public class FileSystemService
    {
        private readonly IOptionsSnapshot<FileDevConfigurationOptions> _snapshot;
        private readonly INotDateTime _notDateTime;
        private readonly ILogger<FileSystemService> _logger;

        public FileSystemService(IOptionsSnapshot<FileDevConfigurationOptions> snapshot, INotDateTime notDateTime, ILogger<FileSystemService> logger)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot), "配置快照不能为空，请检查配置是否正确");
            _notDateTime = notDateTime ?? throw new ArgumentNullException(nameof(notDateTime), "INotDateTime 服务不能为空");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "日志记录器不能为空");
        }

        public async ValueTask<string> UpLoadAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream), "上传的流不能为空");
            }

            try
            {
                // 从配置中获取上传目录
                var uploadPath = _snapshot.Value.fileConfigurationSetting.FilePath;
                if (string.IsNullOrEmpty(uploadPath))
                {
                    throw new InvalidOperationException("上传目录未配置，请检查配置文件");
                }

                // 确保上传目录存在
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // 生成唯一文件名
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(_snapshot.Value.fileConfigurationSetting.FilePath ?? string.Empty)}";
                var filePath = Path.Combine(uploadPath, fileName);

                // 使用文件流写入文件
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
                await stream.CopyToAsync(fileStream, cancellationToken);

                _logger.LogInformation("文件上传成功，路径: {FilePath}", filePath);
                return filePath;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "文件上传时发生IO异常");
                throw new InvalidOperationException("文件上传失败，请检查文件系统权限和路径", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文件上传时发生未知异常");
                throw new InvalidOperationException("文件上传失败，请稍后重试", ex);
            }
        }
    }
}
