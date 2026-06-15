using Scriban;
using Scriban.Runtime;

namespace Notcomd.NotEmail.Templates;

/// <summary>
/// 基于 Scriban 的邮件模板引擎
/// 
/// 支持模板语法:
///   {{ Name }}                   — 变量替换（直接访问模型属性）
///   {{ if condition }} ... {{ end }} — 条件
///   {{ for item in List }} ... {{ end }} — 循环
/// 
/// 说明:
///   Scriban 中模型属性直接在根上下文访问，无需前缀。
///   例如 model = new { Name = "World" } 时模板用 {{ Name }}。
/// </summary>
public class ScribanTemplateEngine : ITemplateEngine
{
    /// <summary>
    /// 渲染模板字符串
    /// </summary>
    /// <param name="templateContent">模板内容（Scriban 语法）</param>
    /// <param name="model">模板数据模型（匿名对象、record、class 均可）</param>
    /// <returns>渲染后的字符串</returns>
    public Task<string> RenderAsync(string templateContent, object model)
    {
        var template = Template.Parse(templateContent);
        if (template.HasErrors)
        {
            var errors = string.Join("; ", template.Messages);
            throw new TemplateRenderException($"模板解析失败: {errors}");
        }

        // 使用 ScriptObject 包装模型以兼容匿名类型
        var scriptObject = new ScriptObject();
        scriptObject.Import(model, renamer: member => member.Name);
        var context = new TemplateContext
        {
            MemberRenamer = member => member.Name,
            EnableRelaxedMemberAccess = true
        };
        context.PushGlobal(scriptObject);

        // 允许循环访问
        context.LoopLimit = 1000;

        var result = template.Render(context);
        return Task.FromResult(result);
    }
}

/// <summary>
/// 模板渲染异常
/// </summary>
public class TemplateRenderException : Exception
{
    public TemplateRenderException(string message) : base(message)
    {
    }

    public TemplateRenderException(string message, Exception inner) : base(message, inner)
    {
    }
}