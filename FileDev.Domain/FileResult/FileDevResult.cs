using System;

namespace FileDev.Domain.FileResult
{
    /// <summary>
    /// 表示文件操作结果的基类，可用于封装文件操作的状态、消息和数据。
    /// </summary>
    public class FileDevResult
    {

        public FileDevResultStatus IsSuccess { get; set; }

        public string Message { get; init; } = string.Empty;

 
        public Exception? Exception { get; init; }

        public string? FilePath { get; init; }

   
        public string? FileName { get; init; }

  
        public long FileSize { get; init; }

 
        public FileDevResult()
        {
            IsSuccess = FileDevResultStatus.Error;
        }

        /// <summary>
        /// 创建一个表示成功的操作结果。
        /// </summary>
        /// <param name="message">成功消息。</param>
        /// <param name="filePath">文件路径。</param>
        /// <param name="fileName">文件名。</param>
        /// <param name="fileSize">文件大小。</param>
        /// <returns>返回一个表示成功的 <see cref="FileDevResult"/> 实例。</returns>
        public static FileDevResult Success(string message = "", string? filePath = null, string? fileName = null, long fileSize = 0)
        {
            return new FileDevResult
            {
                IsSuccess = FileDevResultStatus.Success,
                Message = message,
                FilePath = filePath,
                FileName = fileName,
                FileSize = fileSize
            };
        }

        /// <summary>
        /// 创建一个表示失败的操作结果。
        /// </summary>
        /// <param name="message">失败消息。</param>
        /// <param name="exception">异常信息。</param>
        /// <returns>返回一个表示失败的 <see cref="FileDevResult"/> 实例。</returns>
        public static FileDevResult Fail(string message = "", Exception? exception = null)
        {
            return new FileDevResult
            {
                IsSuccess = FileDevResultStatus.Error,
                Message = message,
                Exception = exception
            };
        }
    }
}
