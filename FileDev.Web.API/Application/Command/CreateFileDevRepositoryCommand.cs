using NotMediator;

namespace FileDev.Web.API.Application.Command
{
    /// <summary>
    /// 创建文件开发仓库的命令类
    /// </summary>
    public sealed class CreateFileDevRepositoryCommand : IRequest<bool>
    {
        /// <summary>
        /// 文件仓库的唯一标识
        /// </summary>
        public Guid FileRepositoryGuid { get; }

        /// <summary>
        /// 文件仓库用户的唯一标识
        /// </summary>
        public Guid FileRepositoryUserGuid { get; }

        /// <summary>
        /// 文件仓库的名称
        /// </summary>
        public string FileRepositoryName { get; }

        /// <summary>
        /// 文件备注信息
        /// </summary>
        public string FileRemarks { get; }

        /// <summary>
        /// 文件仓库封面的 URL
        /// </summary>
        public Uri? FileRepositoryCover { get; }

        /// <summary>
        /// 初始化 <see cref="CreateFileDevRepositoryCommand"/> 类的新实例
        /// </summary>
        /// <param name="fileRepositoryGuid">文件仓库的唯一标识</param>
        /// <param name="fileRepositoryUserGuid">文件仓库用户的唯一标识</param>
        /// <param name="fileRepositoryName">文件仓库的名称</param>
        /// <param name="fileRemarks">文件备注信息</param>
        /// <param name="fileRepositoryCover">文件仓库封面的 URL</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="fileRepositoryName"/> 或 <paramref name="fileRemarks"/> 为 null 或空时抛出</exception>
        public CreateFileDevRepositoryCommand(
            Guid fileRepositoryGuid,
            Guid fileRepositoryUserGuid,
            string fileRepositoryName,
            string fileRemarks,
            Uri? fileRepositoryCover = null)
        {
            FileRepositoryGuid = fileRepositoryGuid;
            FileRepositoryUserGuid = fileRepositoryUserGuid;
            FileRepositoryName = fileRepositoryName ?? throw new ArgumentNullException(nameof(fileRepositoryName), "文件仓库名称不能为空");
            FileRemarks = fileRemarks ?? throw new ArgumentNullException(nameof(fileRemarks), "文件备注信息不能为空");
            FileRepositoryCover = fileRepositoryCover;
        }
    }

 
    
}
