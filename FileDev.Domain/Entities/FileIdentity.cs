namespace FileDev.Domain.Entities;

public enum FileIdentity
{
    /// <summary>
    /// 文件公开
    /// </summary>
    FilePublic,

    /// <summary>
    /// 文件私有
    /// </summary>
    FilePrivate,

    /// <summary>
    /// 文件受限公开
    /// </summary>
    FilePrivatePublic,
    
    /// <summary>
    /// 文件密码保护
    /// </summary>
    FilePasswordProtected
}