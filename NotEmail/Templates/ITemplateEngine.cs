namespace Notcomd.NotEmail.Templates;

/// <summary>
/// 邮件模板引擎接口
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    /// 渲染模板
    /// </summary>
    /// <param name="templateContent">模板内容（Scriban 语法）</param>
    /// <param name="model">数据模型</param>
    /// <returns>渲染后的字符串</returns>
    Task<string> RenderAsync(string templateContent, object model);
}