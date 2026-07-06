namespace Identity.Web.API.Application.Commands;

/// <summary>
/// 可记录命令接口，提供命令标识属性用于日志追踪
/// </summary>
public interface ILoggableCommand
{
    /// <summary>标识属性名（如 Email、UserEmail）</summary>
    string IdProperty { get; }

    /// <summary>标识属性值</summary>
    string IdValue { get; }
}